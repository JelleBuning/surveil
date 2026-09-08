namespace UnifiProtectClient.Application.Settings;

public sealed record UnifiProtectProviderSettings
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string? SnapshotPath { get; init; }
}
