using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Application.Ports;

namespace Surveil.Core.Tests.Application;

[TestClass]
public sealed class StartupTaskStatusExtensionsTests
{
    [TestMethod]
    [DataRow(StartupTaskStatus.Enabled)]
    [DataRow(StartupTaskStatus.EnabledByPolicy)]
    public void IsEnabled_IsTrue_ForEnabledStates(StartupTaskStatus status)
    {
        Assert.IsTrue(status.IsEnabled());
    }

    [TestMethod]
    [DataRow(StartupTaskStatus.Disabled)]
    [DataRow(StartupTaskStatus.DisabledByUser)]
    [DataRow(StartupTaskStatus.DisabledByPolicy)]
    [DataRow(StartupTaskStatus.Unavailable)]
    public void IsEnabled_IsFalse_ForEveryOtherState(StartupTaskStatus status)
    {
        Assert.IsFalse(status.IsEnabled());
    }

    [TestMethod]
    [DataRow(StartupTaskStatus.Enabled)]
    [DataRow(StartupTaskStatus.Disabled)]
    public void CanUserChange_IsTrue_WhenWindowsLeavesTheDecisionToTheApp(StartupTaskStatus status)
    {
        Assert.IsTrue(status.CanUserChange());
    }

    [TestMethod]
    [DataRow(StartupTaskStatus.DisabledByUser)]
    [DataRow(StartupTaskStatus.DisabledByPolicy)]
    [DataRow(StartupTaskStatus.EnabledByPolicy)]
    [DataRow(StartupTaskStatus.Unavailable)]
    public void CanUserChange_IsFalse_WhenTheDecisionIsTakenElsewhere(StartupTaskStatus status)
    {
        Assert.IsFalse(status.CanUserChange());
    }

    [TestMethod]
    [DataRow(StartupTaskStatus.Enabled)]
    [DataRow(StartupTaskStatus.Disabled)]
    public void GetRestrictionDescription_IsNull_WhenTheSettingIsChangeable(StartupTaskStatus status)
    {
        Assert.IsNull(status.GetRestrictionDescription());
    }

    [TestMethod]
    [DataRow(StartupTaskStatus.DisabledByUser)]
    [DataRow(StartupTaskStatus.DisabledByPolicy)]
    [DataRow(StartupTaskStatus.EnabledByPolicy)]
    [DataRow(StartupTaskStatus.Unavailable)]
    public void GetRestrictionDescription_ExplainsWhy_WhenTheSettingIsLocked(StartupTaskStatus status)
    {
        var description = status.GetRestrictionDescription();

        Assert.IsNotNull(description);
        Assert.IsFalse(string.IsNullOrWhiteSpace(description));
    }

    [TestMethod]
    public void EveryLockedStatus_HasItsOwnExplanation()
    {
        StartupTaskStatus[] locked =
        [
            StartupTaskStatus.DisabledByUser,
            StartupTaskStatus.DisabledByPolicy,
            StartupTaskStatus.EnabledByPolicy,
            StartupTaskStatus.Unavailable
        ];

        var descriptions = new System.Collections.Generic.HashSet<string>();

        foreach (var status in locked)
            Assert.IsTrue(descriptions.Add(status.GetRestrictionDescription()!),
                $"{status} reuses another status' description.");
    }
}
