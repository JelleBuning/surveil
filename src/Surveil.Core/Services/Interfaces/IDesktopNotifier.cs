using Surveil.Domain.Events;

namespace Surveil.Services.Interfaces;

public interface IDesktopNotifier
{
    void Notify(CameraEvent cameraEvent, string cameraName);
}
