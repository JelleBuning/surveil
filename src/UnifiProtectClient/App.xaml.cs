using CommunityToolkit.Mvvm.DependencyInjection;
using H.NotifyIcon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using System;
using System.Collections.Generic;
using UnifiProtectClient.Application.Options;
using UnifiProtectClient.Application.Ports;
using UnifiProtectClient.Application.Settings;
using UnifiProtectClient.Infrastructure.Http;
using UnifiProtectClient.Infrastructure.Settings;
using UnifiProtectClient.Infrastructure.WebSocket;
using UnifiProtectClient.Services;
using UnifiProtectClient.Services.Interfaces;
using UnifiProtectClient.ViewModels;
using UnifiProtectClient.Views;

namespace UnifiProtectClient;

public partial class App
{
    private MainWindow? _mainWindow;

    protected override async void OnLaunched(LaunchActivatedEventArgs _)
    {
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var keyInstance = AppInstance.FindOrRegisterForKey("UnifiProtectClient");

        if (!keyInstance.IsCurrent)
        {
            await keyInstance.RedirectActivationToAsync(activationArgs);
            Exit();
            return;
        }

        keyInstance.Activated += OnActivated;

        try
        {
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"App startup failed: {ex.Message}");
            throw;
        }
    }

    private void OnActivated(object? sender, AppActivationArguments args)
    {
        if (args.Kind == ExtendedActivationKind.ToastNotification)
            _mainWindow?.DispatcherQueue.TryEnqueue(_mainWindow.BringToFront);
    }
}
