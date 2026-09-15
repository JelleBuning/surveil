namespace Surveil.Application.Ports;

public enum StartupTaskStatus
{
    Unavailable,
    Disabled,
    DisabledByUser,
    DisabledByPolicy,
    Enabled,
    EnabledByPolicy
}
