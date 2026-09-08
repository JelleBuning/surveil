using Surveil.Domain.Events;

namespace Surveil.Services.Interfaces;

public interface IDesktopNotifier
{
    void Notify(ProtectEvent protectEvent, string cameraName);
}