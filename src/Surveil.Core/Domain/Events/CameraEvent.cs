namespace Surveil.Domain.Events;

public sealed record CameraEvent(string Id, string DeviceId, string Description);
