using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace UnifiProtectClient.Infrastructure.WebSocket;

internal interface IWebSocketConnection : IDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, CancellationToken ct);
    ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken ct);
}
