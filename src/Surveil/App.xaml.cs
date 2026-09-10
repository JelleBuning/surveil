using CommunityToolkit.Mvvm.DependencyInjection;
using H.NotifyIcon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Surveil.Application.Options;
using Surveil.Application.Ports;
using Surveil.Infrastructure.Http;
using Surveil.Infrastructure.Settings;
using Surveil.Infrastructure.WebSocket;
using Surveil.Services;
using Surveil.Services.Interfaces;
using Surveil.ViewModels;
using Surveil.Views;

namespace Surveil;

public partial class App
{
    private MainWindow? _mainWindow;
    private bool _isShowingErrorDialog;

    protected override async void OnLaunched(LaunchActivatedEventArgs _)
    {
        UnhandledException += OnUnhandledException;

        try
        {
            var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            var keyInstance = AppInstance.FindOrRegisterForKey("Surveil");

            if (!keyInstance.IsCurrent)
            {
                await keyInstance.RedirectActivationToAsync(activationArgs);
                Exit();
                return;
            }

            keyInstance.Activated += OnActivated;

            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = AppContext.BaseDirectory
            });

            // Load persisted settings and let them override appsettings.json values
            var settingsRepo = new JsonAppSettingsRepository();
            var appSettings  = await settingsRepo.LoadAsync();

            if (!string.IsNullOrEmpty(appSettings.UnifiProtect.BaseUrl))
            {
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{UnifiProtectOptions.SectionName}:BaseUrl"]      = appSettings.UnifiProtect.BaseUrl,
                    [$"{UnifiProtectOptions.SectionName}:ApiKey"]       = appSettings.UnifiProtect.ApiKey,
                    [$"{UnifiProtectOptions.SectionName}:SnapshotPath"] = appSettings.UnifiProtect.SnapshotPath,
                });
            }

            builder.Services.Configure<UnifiProtectOptions>(builder.Configuration.GetSection(UnifiProtectOptions.SectionName));
            builder.Services.Configure<EventNotificationSettings>(builder.Configuration.GetSection(EventNotificationSettings.SectionName));

            builder.Services.AddSingleton<IAppSettingsRepository>(_ => settingsRepo);
            builder.Services.AddSingleton(appSettings);
            builder.Services.AddSingleton<ISettingsChangeNotifier, SettingsChangeNotifier>();

            builder.Services.AddSingleton<ICameraProvider, ReloadableCameraProvider>();
            builder.Services.AddSingleton<IProtectEventStream, ProtectEventStream>();
            builder.Services.AddTransient<IDesktopNotifier, DesktopNotifier>();
            builder.Services.AddTransient<SettingsViewModel>();

            builder.Services.AddSingleton<MainWindow>();

            var host = builder.Build();
            Ioc.Default.ConfigureServices(host.Services);

            _mainWindow = host.Services.GetRequiredService<MainWindow>();
            _mainWindow.ShowInTaskbar();

            AppNotificationManager.Default.NotificationInvoked += (_, _) => _mainWindow.ShowFromBackground();
            AppNotificationManager.Default.Register();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"App startup failed: {ex.Message}");
        }
    }

    private void OnActivated(object? sender, AppActivationArguments args)
    {
        if (args.Kind == ExtendedActivationKind.ToastNotification)
            _mainWindow?.DispatcherQueue.TryEnqueue(_mainWindow.BringToFront);
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        System.Diagnostics.Debug.WriteLine($"[App] Unhandled exception: {e.Exception}");

        if (_mainWindow is null || _isShowingErrorDialog) return;

        _isShowingErrorDialog = true;
        _mainWindow.DispatcherQueue.TryEnqueue(async () => await ShowErrorDialogAsync());
    }

    private async Task ShowErrorDialogAsync()
    {
        try
        {
            if (_mainWindow?.Content?.XamlRoot is null) return;

            var dialog = new ContentDialog
            {
                Title = "Something went wrong",
                Content = "Surveil ran into an unexpected error and had to recover. If this keeps happening, please report it.",
                CloseButtonText = "OK",
                XamlRoot = _mainWindow.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Failed to show error dialog: {ex.Message}");
        }
        finally
        {
            _isShowingErrorDialog = false;
        }
    }
}
