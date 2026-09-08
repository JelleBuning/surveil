using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnifiProtectClient.Domain.Cameras;

namespace UnifiProtectClient.Application.Ports;

public interface ICameraProvider
{
    Task<IReadOnlyList<Camera>> GetCamerasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RtspsStream>> GetRtspsStreamsAsync(string cameraId, CancellationToken ct = default);
    Task<RtspsStream> CreateRtspsStreamAsync(string cameraId, CancellationToken ct = default);
}
