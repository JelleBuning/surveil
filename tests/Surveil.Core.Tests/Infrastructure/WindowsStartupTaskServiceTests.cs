using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Application.Ports;
using Surveil.Infrastructure.Startup;
using Windows.ApplicationModel;

namespace Surveil.Core.Tests.Infrastructure;

[TestClass]
public sealed class WindowsStartupTaskServiceTests
{
    [TestMethod]
    [DataRow(StartupTaskState.Enabled, StartupTaskStatus.Enabled)]
    [DataRow(StartupTaskState.EnabledByPolicy, StartupTaskStatus.EnabledByPolicy)]
    [DataRow(StartupTaskState.Disabled, StartupTaskStatus.Disabled)]
    [DataRow(StartupTaskState.DisabledByUser, StartupTaskStatus.DisabledByUser)]
    [DataRow(StartupTaskState.DisabledByPolicy, StartupTaskStatus.DisabledByPolicy)]
    public void Map_TranslatesEveryKnownWindowsState(StartupTaskState state, StartupTaskStatus expected)
    {
        Assert.AreEqual(expected, WindowsStartupTaskService.Map(state));
    }

    [TestMethod]
    public void Map_ForAnUnknownState_FallsBackToUnavailable()
    {
        Assert.AreEqual(StartupTaskStatus.Unavailable, WindowsStartupTaskService.Map((StartupTaskState)999));
    }

    [TestMethod]
    public void StartupTaskId_MatchesThePackageManifest()
    {
        Assert.AreEqual("SurveilStartupTask", WindowsStartupTaskService.StartupTaskId);
    }
}
