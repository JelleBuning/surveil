using System.Collections.Generic;
using System.Threading;
using Surveil.Domain.Events;

namespace Surveil.Application.Ports;

public interface ICameraEventStream
{
    IAsyncEnumerable<CameraEvent> SubscribeAsync(CancellationToken ct = default);
}
