using System.Diagnostics.CodeAnalysis;

namespace UnifiProtectClient.Infrastructure.WebSocket;

[ExcludeFromCodeCoverage]
internal sealed class ClientWebSocketFactory : IWebSocketFactory
{
    public IWebSocketConnection Create(string apiKey) => new ClientWebSocketWrapper(apiKey);
}
