namespace Surveil.Unifi.WebSocket;

internal interface IWebSocketFactory
{
    IWebSocketConnection Create(string apiKey);
}
