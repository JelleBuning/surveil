namespace Surveil.Application.Options;

public sealed class UnifiProtectOptions
{
    public const string SectionName = "UnifiProtect";

    /// <summary>Fixed path of the Protect integration API, appended to <see cref="BaseUrl"/>.</summary>
    public const string ApiPath = "proxy/protect/integration";

    /// <summary>Console address only (e.g. "https://192.168.0.1") — no path.</summary>
    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
    public string? SnapshotPath { get; init; }
}
