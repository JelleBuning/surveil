using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Cameras;

namespace Surveil.Infrastructure.Http;

public sealed class ReloadableCameraProvider : ICameraProvider
{
    private readonly Lock _lock = new();
    private readonly IReadOnlyList<ICameraProviderFactory> _factories;
    private ICameraProvider _current;

    public ReloadableCameraProvider(
        AppSettings initialSettings,
        ISettingsChangeNotifier notifier,
        IEnumerable<ICameraProviderFactory> factories)
    {
        _factories = factories.ToList();
        _current = Build(initialSettings);

        notifier.SettingsChanged += settings =>
        {
            lock (_lock)
                _current = Build(settings);
        };
    }

    public Task<IReadOnlyList<Camera>> GetCamerasAsync(CancellationToken ct = default) =>
        Current.GetCamerasAsync(ct);

    public Task<IReadOnlyList<RtspsStream>> GetRtspsStreamsAsync(string cameraId, CancellationToken ct = default) =>
        Current.GetRtspsStreamsAsync(cameraId, ct);

    public Task<RtspsStream> CreateRtspsStreamAsync(string cameraId, CancellationToken ct = default) =>
        Current.CreateRtspsStreamAsync(cameraId, ct);

    private ICameraProvider Current
    {
        get { lock (_lock) return _current; }
    }

    private ICameraProvider Build(AppSettings settings)
    {
        var factory = _factories.FirstOrDefault(f => f.ProviderType == settings.SelectedProvider);
        return factory?.Create(settings) ?? new NoOpCameraProvider();
    }
}
