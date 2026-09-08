namespace UnifiProtectClient.Application.Settings;

public sealed record AppSettings
{
    public VideoProviderType SelectedProvider { get; init; } = VideoProviderType.UnifiProtect;
    public UnifiProtectProviderSettings UnifiProtect { get; init; } = new();
}
