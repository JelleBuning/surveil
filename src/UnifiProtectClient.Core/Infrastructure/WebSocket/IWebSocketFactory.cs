namespace UnifiProtectClient.Infrastructure.WebSocket;

internal interface IWebSocketFactory
{
    IWebSocketConnection Create(string apiKey);
}
