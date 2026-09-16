using System;
using System.Diagnostics;
using System.IO;
using Surveil.Application.Settings;
using Surveil.Domain.Events;
using Surveil.Services.Interfaces;

namespace Surveil.Services;

public sealed class DesktopNotifier : IDesktopNotifier
{
    private readonly string _snapshotPath;
    private readonly IAppNotificationSender _sender;

    public DesktopNotifier(SnapshotOptions snapshot)
        : this(snapshot, new AppNotificationSender()) { }

    internal DesktopNotifier(SnapshotOptions snapshot, IAppNotificationSender sender)
    {
        _snapshotPath = snapshot.Path;
        _sender = sender;
    }

    public void Notify(CameraEvent cameraEvent, string cameraName)
    {
        try
        {
            var title = BuildTitle(cameraEvent, cameraName);
            var heroPath = SnapshotService.GetHeroPath(_snapshotPath);
            _sender.Notify(title, File.Exists(heroPath) ? heroPath : null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing notification: {ex.Message}");
        }
    }

    internal static string BuildTitle(CameraEvent cameraEvent, string cameraName) =>
        $"{cameraEvent.Description} ({cameraName})";
}
