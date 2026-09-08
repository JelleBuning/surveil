using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnifiProtectClient.Application.Options;
using UnifiProtectClient.Application.Ports;
using UnifiProtectClient.Application.Settings;

namespace UnifiProtectClient.ViewModels;

public sealed record VideoProviderOption(VideoProviderType Type, string DisplayName);

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingsRepository _repository;

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
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsUnifiProviderSelected));
                OnPropertyChanged(nameof(UnifiProviderVisibility));
            }
        }
    }

    public bool IsUnifiProviderSelected =>
        SelectedProvider.Type == VideoProviderType.UnifiProtect;

    public Visibility UnifiProviderVisibility =>
        IsUnifiProviderSelected ? Visibility.Visible : Visibility.Collapsed;

    public string BaseUrl
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string ApiKey
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string SnapshotPath
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public bool ShowSuccess
    {
        get;
        set => SetProperty(ref field, value);
    }

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
        IOptions<UnifiProtectOptions> currentOptions,
        IAppSettingsRepository repository)
    {
        _repository = repository;

        var opts = currentOptions.Value;
        SelectedProvider = AvailableProviders[0];
        BaseUrl          = opts.BaseUrl;
        ApiKey           = opts.ApiKey;
        SnapshotPath     = opts.SnapshotPath ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ShowSuccess = false;
        ShowError   = false;

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
        ShowSuccess = true;
    }

    private bool Validate()
    {
        if (SelectedProvider.Type != VideoProviderType.UnifiProtect)
            return true;

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            ErrorMessage = "Base URL is required.";
            ShowError    = true;
            return false;
        }

        if (!Uri.TryCreate(BaseUrl.Trim(), UriKind.Absolute, out _))
        {
            ErrorMessage = "Base URL must be a valid absolute URL (e.g. https://192.168.1.1/proxy/protect/integration).";
            ShowError    = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ErrorMessage = "API Key is required.";
            ShowError    = true;
            return false;
        }

        return true;
    }
}
