namespace Surveil.Unifi;

public sealed class UnifiProtectOptions
{
    public const string ApiPath = "proxy/protect/integration";
    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
}
