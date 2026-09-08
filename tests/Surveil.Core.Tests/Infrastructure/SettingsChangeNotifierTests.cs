using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Application.Settings;
using Surveil.Infrastructure.Settings;

namespace Surveil.Core.Tests.Infrastructure;

[TestClass]
public sealed class SettingsChangeNotifierTests
{
    [TestMethod]
    public void NotifyChanged_InvokesSubscribersWithSettings()
    {
        var notifier = new SettingsChangeNotifier();
        AppSettings? received = null;
        notifier.SettingsChanged += settings => received = settings;

        var sent = new AppSettings { SelectedProvider = VideoProviderType.UnifiProtect };
        notifier.NotifyChanged(sent);

        Assert.AreSame(sent, received);
    }

    [TestMethod]
    public void NotifyChanged_NoSubscribers_DoesNotThrow()
    {
        var notifier = new SettingsChangeNotifier();
        notifier.NotifyChanged(new AppSettings());
    }
}
