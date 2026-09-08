namespace Surveil.Application.Settings;

public sealed record AppSettings
{
    public VideoProviderType SelectedProvider { get; init; } = VideoProviderType.None;
    public UnifiProtectProviderSettings UnifiProtect { get; init; } = new();
}
