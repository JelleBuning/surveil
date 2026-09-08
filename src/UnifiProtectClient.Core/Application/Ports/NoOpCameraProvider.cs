using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnifiProtectClient.Domain.Cameras;

namespace UnifiProtectClient.Application.Ports;

/// <summary>Used when no video provider is configured (VideoProviderType.None).</summary>
public sealed class NoOpCameraProvider : ICameraProvider
{
    public Task<IReadOnlyList<Camera>> GetCamerasAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Camera>>([]);

    public Task<IReadOnlyList<RtspsStream>> GetRtspsStreamsAsync(string cameraId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RtspsStream>>([]);

    public Task<RtspsStream> CreateRtspsStreamAsync(string cameraId, CancellationToken ct = default) =>
        throw new NotSupportedException("No camera provider is configured.");
}
