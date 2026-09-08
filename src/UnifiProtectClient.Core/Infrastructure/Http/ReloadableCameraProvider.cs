using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnifiProtectClient.Application.Options;
using UnifiProtectClient.Application.Ports;
using UnifiProtectClient.Application.Settings;
using UnifiProtectClient.Domain.Cameras;

namespace UnifiProtectClient.Infrastructure.Http;

/// <summary>
/// Delegates to whichever ICameraProvider matches the current settings, swapping it
/// live when settings are saved — so callers never need the app restarted to pick up
/// a changed provider, base URL, or API key.
/// </summary>
public sealed class ReloadableCameraProvider : ICameraProvider
{
    private readonly object _lock = new();
    private ICameraProvider _current;

    public ReloadableCameraProvider(AppSettings initialSettings, ISettingsChangeNotifier notifier)
    {
        _current = Build(initialSettings);
        notifier.SettingsChanged += settings =>
        {
            lock (_lock)
                _current = Build(settings);
        };
    }

    private static ICameraProvider Build(AppSettings settings) => settings.SelectedProvider switch
    {
        VideoProviderType.UnifiProtect => new UnifiProtectApiClient(Options.Create(new UnifiProtectOptions
        {
            BaseUrl      = settings.UnifiProtect.BaseUrl,
            ApiKey       = settings.UnifiProtect.ApiKey,
            SnapshotPath = settings.UnifiProtect.SnapshotPath
        })),
        _ => new NoOpCameraProvider()
    };

    private ICameraProvider Current
    {
        get { lock (_lock) return _current; }
    }

    public Task<IReadOnlyList<Camera>> GetCamerasAsync(CancellationToken ct = default) =>
        Current.GetCamerasAsync(ct);

    public Task<IReadOnlyList<RtspsStream>> GetRtspsStreamsAsync(string cameraId, CancellationToken ct = default) =>
        Current.GetRtspsStreamsAsync(cameraId, ct);

    public Task<RtspsStream> CreateRtspsStreamAsync(string cameraId, CancellationToken ct = default) =>
        Current.CreateRtspsStreamAsync(cameraId, ct);
}
