using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Application.Ports;

namespace Surveil.Core.Tests.Application;

[TestClass]
public sealed class StartupTaskStatusExtensionsTests
{
    [TestMethod]
    [DataRow(StartupTaskStatus.Enabled)]
    [DataRow(StartupTaskStatus.EnabledByPolicy)]
    public void IsEnabled_ForAnEnabledState_IsTrue(StartupTaskStatus status)
    {
        Assert.IsTrue(status.IsEnabled());
    }

    [TestMethod]
    [DataRow(StartupTaskStatus.Disabled)]
    [DataRow(StartupTaskStatus.DisabledByUser)]
    [DataRow(StartupTaskStatus.DisabledByPolicy)]
    [DataRow(StartupTaskStatus.Unavailable)]
    public void IsEnabled_ForEveryOtherState_IsFalse(StartupTaskStatus status)
    {
        Assert.IsFalse(status.IsEnabled());
    }

    [TestMethod]
    [DataRow(StartupTaskStatus.Enabled)]
    [DataRow(StartupTaskStatus.Disabled)]
    public void CanUserChange_WhenWindowsLeavesTheDecisionToTheApp_IsTrue(StartupTaskStatus status)
    {
        Assert.IsTrue(status.CanUserChange());
    }

    [TestMethod]
    [DataRow(StartupTaskStatus.DisabledByUser)]
    [DataRow(StartupTaskStatus.DisabledByPolicy)]
    [DataRow(StartupTaskStatus.EnabledByPolicy)]
    [DataRow(StartupTaskStatus.Unavailable)]
    public void CanUserChange_WhenTheDecisionIsTakenElsewhere_IsFalse(StartupTaskStatus status)
    {
        Assert.IsFalse(status.CanUserChange());
    }

    [TestMethod]
    [DataRow(StartupTaskStatus.Enabled)]
    [DataRow(StartupTaskStatus.Disabled)]
    public void GetRestrictionDescription_WhenTheSettingIsChangeable_IsNull(StartupTaskStatus status)
    {
        Assert.IsNull(status.GetRestrictionDescription());
    }

    [TestMethod]
    [DataRow(StartupTaskStatus.DisabledByUser)]
    [DataRow(StartupTaskStatus.DisabledByPolicy)]
    [DataRow(StartupTaskStatus.EnabledByPolicy)]
    [DataRow(StartupTaskStatus.Unavailable)]
    public void GetRestrictionDescription_WhenTheSettingIsLocked_ExplainsWhy(StartupTaskStatus status)
    {
        var description = status.GetRestrictionDescription();

        Assert.IsNotNull(description);
        Assert.IsFalse(string.IsNullOrWhiteSpace(description));
    }

    [TestMethod]
    public void GetRestrictionDescription_ForEveryLockedStatus_IsDistinct()
    {
        StartupTaskStatus[] locked =
        [
            StartupTaskStatus.DisabledByUser,
            StartupTaskStatus.DisabledByPolicy,
            StartupTaskStatus.EnabledByPolicy,
            StartupTaskStatus.Unavailable
        ];

        var descriptions = new HashSet<string>();

        foreach (var status in locked)
        {
            var description = status.GetRestrictionDescription();

            Assert.IsNotNull(description);
            Assert.IsTrue(descriptions.Add(description), $"{status} reuses another status' description.");
        }
    }
}
