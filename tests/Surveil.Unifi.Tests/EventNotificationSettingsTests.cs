using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Surveil.Unifi.Tests;

[TestClass]
public sealed class EventNotificationSettingsTests
{
    private static MotionEvent Motion() => new("id", 0, null, "dev", ProtectEventUpdateType.Add);
    private static SmartDetectZoneEvent SmartDetectZone() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, []);
    private static SmartDetectLineEvent SmartDetectLine() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, []);
    private static SmartDetectLoiterZoneEvent SmartDetectLoiterZone() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, []);
    private static SmartAudioDetectEvent SmartAudioDetect() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, []);
    private static RingEvent Ring() => new("id", 0, null, "dev", ProtectEventUpdateType.Add);
    private static LightMotionEvent LightMotion() => new("id", 0, "dev", ProtectEventUpdateType.Add);
    private static SensorMotionEvent SensorMotion() => new("id", 0, null, "dev", ProtectEventUpdateType.Add);
    private static SensorTamperEvent SensorTamper() => new("id", 0, null, "dev", ProtectEventUpdateType.Add);
    private static SensorSmokeTestEvent SensorSmokeTest() => new("id", 0, null, "dev", ProtectEventUpdateType.Add);
    private static SensorAlarmEvent SensorAlarm() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, "smoke");
    private static SensorOpenedEvent SensorOpened() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, "door");
    private static SensorClosedEvent SensorClosed() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, "window");
    private static SensorWaterLeakEvent SensorWaterLeak() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, "leak");
    private static SensorBatteryLowEvent SensorBatteryLow() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, 15.0);
    private static SensorExtremeValuesEvent SensorExtremeValues() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, "temperature", 45.0, "high");
    private static UnknownEvent Unknown() => new("id", "weird", 0, null, "dev", ProtectEventUpdateType.Add);

    [TestMethod]
    public void Defaults_OnlyRingIsTrue()
    {
        var settings = new EventNotificationSettings();

        Assert.IsFalse(settings.Motion);
        Assert.IsFalse(settings.SmartDetectZone);
        Assert.IsFalse(settings.SmartDetectLine);
        Assert.IsFalse(settings.SmartDetectLoiterZone);
        Assert.IsFalse(settings.SmartAudioDetect);
        Assert.IsTrue(settings.Ring);
        Assert.IsFalse(settings.LightMotion);
        Assert.IsFalse(settings.SensorMotion);
        Assert.IsFalse(settings.SensorTamper);
        Assert.IsFalse(settings.SensorSmokeTest);
        Assert.IsFalse(settings.SensorAlarm);
        Assert.IsFalse(settings.SensorOpened);
        Assert.IsFalse(settings.SensorClosed);
        Assert.IsFalse(settings.SensorWaterLeak);
        Assert.IsFalse(settings.SensorBatteryLow);
        Assert.IsFalse(settings.SensorExtremeValues);
    }

    [TestMethod]
    public void IsEnabled_MotionDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(Motion()));

    [TestMethod]
    public void IsEnabled_MotionEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { Motion = true }.IsEnabled(Motion()));

    [TestMethod]
    public void IsEnabled_SmartDetectZoneDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SmartDetectZone()));

    [TestMethod]
    public void IsEnabled_SmartDetectZoneEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SmartDetectZone = true }.IsEnabled(SmartDetectZone()));

    [TestMethod]
    public void IsEnabled_SmartDetectLineDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SmartDetectLine()));

    [TestMethod]
    public void IsEnabled_SmartDetectLineEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SmartDetectLine = true }.IsEnabled(SmartDetectLine()));

    [TestMethod]
    public void IsEnabled_SmartDetectLoiterZoneDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SmartDetectLoiterZone()));

    [TestMethod]
    public void IsEnabled_SmartDetectLoiterZoneEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SmartDetectLoiterZone = true }.IsEnabled(SmartDetectLoiterZone()));

    [TestMethod]
    public void IsEnabled_SmartAudioDetectDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SmartAudioDetect()));

    [TestMethod]
    public void IsEnabled_SmartAudioDetectEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SmartAudioDetect = true }.IsEnabled(SmartAudioDetect()));

    [TestMethod]
    public void IsEnabled_RingEnabledByDefault_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings().IsEnabled(Ring()));

    [TestMethod]
    public void IsEnabled_RingDisabled_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings { Ring = false }.IsEnabled(Ring()));

    [TestMethod]
    public void IsEnabled_LightMotionDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(LightMotion()));

    [TestMethod]
    public void IsEnabled_LightMotionEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { LightMotion = true }.IsEnabled(LightMotion()));

    [TestMethod]
    public void IsEnabled_SensorMotionDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SensorMotion()));

    [TestMethod]
    public void IsEnabled_SensorMotionEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SensorMotion = true }.IsEnabled(SensorMotion()));

    [TestMethod]
    public void IsEnabled_SensorTamperDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SensorTamper()));

    [TestMethod]
    public void IsEnabled_SensorTamperEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SensorTamper = true }.IsEnabled(SensorTamper()));

    [TestMethod]
    public void IsEnabled_SensorSmokeTestDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SensorSmokeTest()));

    [TestMethod]
    public void IsEnabled_SensorSmokeTestEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SensorSmokeTest = true }.IsEnabled(SensorSmokeTest()));

    [TestMethod]
    public void IsEnabled_SensorAlarmDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SensorAlarm()));

    [TestMethod]
    public void IsEnabled_SensorAlarmEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SensorAlarm = true }.IsEnabled(SensorAlarm()));

    [TestMethod]
    public void IsEnabled_SensorOpenedDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SensorOpened()));

    [TestMethod]
    public void IsEnabled_SensorOpenedEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SensorOpened = true }.IsEnabled(SensorOpened()));

    [TestMethod]
    public void IsEnabled_SensorClosedDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SensorClosed()));

    [TestMethod]
    public void IsEnabled_SensorClosedEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SensorClosed = true }.IsEnabled(SensorClosed()));

    [TestMethod]
    public void IsEnabled_SensorWaterLeakDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SensorWaterLeak()));

    [TestMethod]
    public void IsEnabled_SensorWaterLeakEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SensorWaterLeak = true }.IsEnabled(SensorWaterLeak()));

    [TestMethod]
    public void IsEnabled_SensorBatteryLowDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SensorBatteryLow()));

    [TestMethod]
    public void IsEnabled_SensorBatteryLowEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SensorBatteryLow = true }.IsEnabled(SensorBatteryLow()));

    [TestMethod]
    public void IsEnabled_SensorExtremeValuesDisabledByDefault_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings().IsEnabled(SensorExtremeValues()));

    [TestMethod]
    public void IsEnabled_SensorExtremeValuesEnabled_ReturnsTrue()
        => Assert.IsTrue(new EventNotificationSettings { SensorExtremeValues = true }.IsEnabled(SensorExtremeValues()));

    [TestMethod]
    public void IsEnabled_UnknownEvent_ReturnsFalse()
        => Assert.IsFalse(new EventNotificationSettings { Ring = true }.IsEnabled(Unknown()));
}
