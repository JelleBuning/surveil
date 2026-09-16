using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Surveil.Application.Settings;
using Surveil.Domain.Events;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestClass]
public sealed class DesktopNotifierTests
{
    private static DesktopNotifier CreateNotifier(
        string? snapshotPath = null,
        IAppNotificationSender? sender = null)
    {
        sender ??= new Mock<IAppNotificationSender>().Object;
        var path = snapshotPath ?? @"C:
onexistent\path\snapshot.jpg";
        return new DesktopNotifier(new SnapshotOptions(path), sender);
    }

    // ── Notify ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Notify_CallsSender_WithTitleAndNoHero_WhenHeroDoesNotExist()
    {
        // Arrange
        var senderMock = new Mock<IAppNotificationSender>();
        var notifier = CreateNotifier(snapshotPath: @"C:\nonexistent\path\snapshot.jpg", sender: senderMock.Object);
        var ev = new CameraEvent("id", "dev1", "Doorbell ring");

        // Act
        notifier.Notify(ev, "Front Door");

        // Assert — hero path doesn't exist, so null is passed
        senderMock.Verify(s => s.Notify("Doorbell ring (Front Door)", null), Times.Once());
    }

    [TestMethod]
    public void Notify_SenderThrows_ExceptionSuppressed()
    {
        // Arrange
        var senderMock = new Mock<IAppNotificationSender>();
        senderMock.Setup(s => s.Notify(It.IsAny<string>(), It.IsAny<string?>()))
                  .Throws(new System.InvalidOperationException("WinRT not initialized"));
        var notifier = CreateNotifier(sender: senderMock.Object);
        var ev = new CameraEvent("id", "dev1", "Doorbell ring");

        // Act — should not throw
        notifier.Notify(ev, "Front Door");
    }
}
