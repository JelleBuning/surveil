using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnifiProtectClient.Application.Settings;
using UnifiProtectClient.Infrastructure.Http;
using UnifiProtectClient.Infrastructure.Settings;

namespace UnifiProtectClient.Core.Tests.Infrastructure;

[TestClass]
public sealed class ReloadableCameraProviderTests
{
    private static AppSettings NoneSettings() => new() { SelectedProvider = VideoProviderType.None };

    private static AppSettings UnifiSettings() => new()
    {
        SelectedProvider = VideoProviderType.UnifiProtect,
        UnifiProtect = new UnifiProtectProviderSettings { BaseUrl = "https://host", ApiKey = "key" }
    };

    [TestMethod]
    public async Task InitialSettingsNone_DelegatesToNoOpProvider()
    {
        var notifier = new SettingsChangeNotifier();
        var provider = new ReloadableCameraProvider(NoneSettings(), notifier);

        var cameras = await provider.GetCamerasAsync();

        Assert.IsEmpty(cameras);
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.CreateRtspsStreamAsync("cam1"));
    }

    [TestMethod]
    public async Task SettingsChanged_ToNone_SwapsToNoOpProvider()
    {
        var notifier = new SettingsChangeNotifier();
        var provider = new ReloadableCameraProvider(UnifiSettings(), notifier);

        notifier.NotifyChanged(NoneSettings());

        var cameras = await provider.GetCamerasAsync();
        Assert.IsEmpty(cameras);
    }
}
