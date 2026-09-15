namespace Surveil.Application.Ports;

public static class StartupTaskStatusExtensions
{
    public static bool IsEnabled(this StartupTaskStatus status) =>
        status is StartupTaskStatus.Enabled or StartupTaskStatus.EnabledByPolicy;

    public static bool CanUserChange(this StartupTaskStatus status) =>
        status is StartupTaskStatus.Enabled or StartupTaskStatus.Disabled;

    public static string? GetRestrictionDescription(this StartupTaskStatus status) => status switch
    {
        StartupTaskStatus.DisabledByUser =>
            "Startup was turned off for Surveil outside the app. Turn it back on under Startup apps in Task Manager.",
        StartupTaskStatus.DisabledByPolicy =>
            "Your organisation's policy prevents Surveil from starting when you sign in.",
        StartupTaskStatus.EnabledByPolicy =>
            "Your organisation's policy requires Surveil to start when you sign in.",
        StartupTaskStatus.Unavailable =>
            "Starting at sign-in is only available when Surveil is run as an installed app.",
        _ => null
    };
}
