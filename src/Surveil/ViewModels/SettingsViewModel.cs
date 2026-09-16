using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Ports;
using Surveil.Application.Settings;

namespace Surveil.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private const string LaunchOnStartupDefaultDescription = "Start Surveil automatically when you sign in to Windows.";
    private readonly IAppSettingsRepository _repository;
    private readonly ISettingsChangeNotifier _notifier;
    private readonly IStartupTaskService _startupTaskService;

    private bool _isSyncingStartupStatus;
    private readonly bool _isLoaded;

    public bool LaunchOnStartup
    {
        get;
        set
        {
            if (!SetProperty(ref field, value) || _isSyncingStartupStatus) return;

            _ = ApplyLaunchOnStartupAsync(value);
        }
    }

    public StartupTaskStatus StartupStatus
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsLaunchOnStartupToggleEnabled));
                OnPropertyChanged(nameof(LaunchOnStartupDescription));
            }
        }
    } = StartupTaskStatus.Unavailable;

    public bool IsLaunchOnStartupToggleEnabled => StartupStatus.CanUserChange();

    public string LaunchOnStartupDescription => StartupStatus.GetRestrictionDescription() ?? LaunchOnStartupDefaultDescription;

    public IReadOnlyList<VideoProviderOption> AvailableProviders { get; } =
    [
        new(VideoProviderType.None,          "None"),
        new(VideoProviderType.UnifiProtect,  "UniFi Protect")
    ];

    public VideoProviderOption SelectedProvider
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;

            OnPropertyChanged(nameof(IsUnifiProviderSelected));
            OnPropertyChanged(nameof(UnifiProviderVisibility));
            SaveIfLoaded();
        }
    }

    public bool IsUnifiProviderSelected =>
        SelectedProvider.Type == VideoProviderType.UnifiProtect;

    public Visibility UnifiProviderVisibility =>
        IsUnifiProviderSelected ? Visibility.Visible : Visibility.Collapsed;

    public string BaseUrl
    {
        get;
        set { if (SetProperty(ref field, value)) SaveIfLoaded(); }
    } = string.Empty;

    public string ApiKey
    {
        get;
        set { if (SetProperty(ref field, value)) SaveIfLoaded(); }
    } = string.Empty;

    public string SnapshotPath
    {
        get;
        set { if (SetProperty(ref field, value)) SaveIfLoaded(); }
    } = string.Empty;

    public bool ShowError
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string ErrorMessage
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public SettingsViewModel(
        AppSettings currentSettings,
        IAppSettingsRepository repository,
        ISettingsChangeNotifier notifier,
        IStartupTaskService startupTaskService)
    {
        _repository         = repository;
        _notifier           = notifier;
        _startupTaskService = startupTaskService;

        SelectedProvider = AvailableProviders.FirstOrDefault(p => p.Type == currentSettings.SelectedProvider)
                            ?? AvailableProviders[0];
        BaseUrl          = currentSettings.UnifiProtect.BaseUrl;
        ApiKey           = currentSettings.UnifiProtect.ApiKey;
        SnapshotPath     = currentSettings.UnifiProtect.SnapshotPath ?? string.Empty;

        _isLoaded = true;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        SyncStartupStatus(await _startupTaskService.GetStatusAsync(ct));
    }

    private async Task ApplyLaunchOnStartupAsync(bool enable)
    {
        var status = enable
            ? await _startupTaskService.EnableAsync()
            : await _startupTaskService.DisableAsync();

        SyncStartupStatus(status);
    }

    private void SyncStartupStatus(StartupTaskStatus status)
    {
        StartupStatus = status;

        _isSyncingStartupStatus = true;
        try
        {
            LaunchOnStartup = status.IsEnabled();
        }
        finally
        {
            _isSyncingStartupStatus = false;
        }
    }

    private void SaveIfLoaded()
    {
        if (!_isLoaded) return;

        _ = SaveAsync(CancellationToken.None);
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        ShowError = false;

        if (!Validate()) return;

        var settings = new AppSettings
        {
            SelectedProvider = SelectedProvider.Type,
            UnifiProtect = new UnifiProtectProviderSettings
            {
                BaseUrl      = BaseUrl.Trim(),
                ApiKey       = ApiKey.Trim(),
                SnapshotPath = string.IsNullOrWhiteSpace(SnapshotPath) ? null : SnapshotPath.Trim()
            }
        };

        await _repository.SaveAsync(settings, ct);
        _notifier.NotifyChanged(settings);
    }

    private bool Validate()
    {
        if (SelectedProvider.Type != VideoProviderType.UnifiProtect)
            return true;

        if (string.IsNullOrWhiteSpace(BaseUrl))
            return Invalid("Base URL is required.");

        if (!Uri.TryCreate(BaseUrl.Trim(), UriKind.Absolute, out _))
            return Invalid("Base URL must be a valid absolute URL (e.g. https://192.168.0.1).");

        if (string.IsNullOrWhiteSpace(ApiKey))
            return Invalid("API Key is required.");

        return true;
    }

    private bool Invalid(string message)
    {
        ErrorMessage = message;
        ShowError    = true;
        return false;
    }
}
