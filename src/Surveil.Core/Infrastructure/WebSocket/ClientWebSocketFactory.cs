using System.Diagnostics.CodeAnalysis;

namespace Surveil.Infrastructure.WebSocket;

[ExcludeFromCodeCoverage]
internal sealed class ClientWebSocketFactory : IWebSocketFactory
{
    public IWebSocketConnection Create(string apiKey) => new ClientWebSocketWrapper(apiKey);
}
