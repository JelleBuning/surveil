using System.Collections.Generic;
using System.Threading;
using Surveil.Domain.Events;

namespace Surveil.Application.Ports;

public interface IProtectEventStream
{
    IAsyncEnumerable<ProtectEvent> SubscribeAsync(CancellationToken ct = default);
}
