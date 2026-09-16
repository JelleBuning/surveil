using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Surveil.Unifi.Tests;

[TestClass]
public sealed class ProtectEventTests
{
    private const string Id = "event-1";
    private const string DeviceId = "device-1";
    private const long Start = 1_700_000_000_000L;
    private const long End = 1_700_000_005_000L;

    [TestMethod]
    public void MotionEvent_TypeIsMotion()
    {
        var ev = new MotionEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add);

        Assert.AreEqual("motion", ev.Type);
        Assert.AreEqual(Id, ev.Id);
        Assert.AreEqual(Start, ev.Start);
        Assert.AreEqual(End, ev.End);
        Assert.AreEqual(DeviceId, ev.DeviceId);
        Assert.AreEqual(ProtectEventUpdateType.Add, ev.UpdateType);
    }

    [TestMethod]
    public void MotionEvent_WithNullEnd_HasNullEnd()
    {
        var ev = new MotionEvent(Id, Start, null, DeviceId, ProtectEventUpdateType.Update);

        Assert.IsNull(ev.End);
    }

    [TestMethod]
    public void SmartDetectZoneEvent_TypeAndSmartDetectTypes()
    {
        var ev = new SmartDetectZoneEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, ["person", "vehicle"]);

        Assert.AreEqual("smartDetectZone", ev.Type);
        CollectionAssert.AreEqual(new[] { "person", "vehicle" }, ev.SmartDetectTypes.ToArray());
    }

    [TestMethod]
    public void SmartDetectLineEvent_TypeIsSmartDetectLine()
    {
        var ev = new SmartDetectLineEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, ["car"]);

        Assert.AreEqual("smartDetectLine", ev.Type);
        Assert.AreEqual(1, ev.SmartDetectTypes.Count);
    }

    [TestMethod]
    public void SmartDetectLoiterZoneEvent_TypeIsSmartDetectLoiterZone()
    {
        var ev = new SmartDetectLoiterZoneEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Update, []);

        Assert.AreEqual("smartDetectLoiterZone", ev.Type);
        Assert.IsEmpty(ev.SmartDetectTypes);
    }

    [TestMethod]
    public void SmartAudioDetectEvent_TypeIsSmartAudioDetect()
    {
        var ev = new SmartAudioDetectEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, ["smoke"]);

        Assert.AreEqual("smartAudioDetect", ev.Type);
    }

    [TestMethod]
    public void RingEvent_TypeIsRing()
    {
        var ev = new RingEvent(Id, Start, null, DeviceId, ProtectEventUpdateType.Add);

        Assert.AreEqual("ring", ev.Type);
        Assert.IsNull(ev.End);
    }

    [TestMethod]
    public void LightMotionEvent_TypeIsLightMotionAndEndIsNull()
    {
        var ev = new LightMotionEvent(Id, Start, DeviceId, ProtectEventUpdateType.Add);

        Assert.AreEqual("lightMotion", ev.Type);
        Assert.IsNull(ev.End);
    }

    [TestMethod]
    public void SensorMotionEvent_TypeIsSensorMotion()
    {
        var ev = new SensorMotionEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add);

        Assert.AreEqual("sensorMotion", ev.Type);
    }

    [TestMethod]
    public void SensorTamperEvent_TypeIsSensorTamper()
    {
        var ev = new SensorTamperEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Update);

        Assert.AreEqual("sensorTamper", ev.Type);
    }

    [TestMethod]
    public void SensorSmokeTestEvent_TypeIsSensorSmokeTest()
    {
        var ev = new SensorSmokeTestEvent(Id, Start, null, DeviceId, ProtectEventUpdateType.Add);

        Assert.AreEqual("sensorSmokeTest", ev.Type);
    }

    [TestMethod]
    public void SensorAlarmEvent_TypeAndAlarmType()
    {
        var ev = new SensorAlarmEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, "smoke");

        Assert.AreEqual("sensorAlarm", ev.Type);
        Assert.AreEqual("smoke", ev.AlarmType);
    }

    [TestMethod]
    public void SensorOpenedEvent_TypeAndMountType()
    {
        var ev = new SensorOpenedEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, "door");

        Assert.AreEqual("sensorOpened", ev.Type);
        Assert.AreEqual("door", ev.MountType);
    }

    [TestMethod]
    public void SensorClosedEvent_TypeAndMountType()
    {
        var ev = new SensorClosedEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, "window");

        Assert.AreEqual("sensorClosed", ev.Type);
        Assert.AreEqual("window", ev.MountType);
    }

    [TestMethod]
    public void SensorWaterLeakEvent_TypeAndMountType()
    {
        var ev = new SensorWaterLeakEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, "leak");

        Assert.AreEqual("sensorWaterLeak", ev.Type);
        Assert.AreEqual("leak", ev.MountType);
    }

    [TestMethod]
    public void SensorBatteryLowEvent_TypeAndBatteryPercentage()
    {
        var ev = new SensorBatteryLowEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, 12.5);

        Assert.AreEqual("sensorBatteryLow", ev.Type);
        Assert.AreEqual(12.5, ev.BatteryPercentage);
    }

    [TestMethod]
    public void SensorExtremeValuesEvent_AllFields()
    {
        var ev = new SensorExtremeValuesEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, "temperature", 42.1, "high");

        Assert.AreEqual("sensorExtremeValues", ev.Type);
        Assert.AreEqual("temperature", ev.SensorType);
        Assert.AreEqual(42.1, ev.SensorValue);
        Assert.AreEqual("high", ev.Status);
    }

    [TestMethod]
    public void UnknownEvent_TypeReflectsRawType()
    {
        var ev = new UnknownEvent(Id, "customType", Start, End, DeviceId, ProtectEventUpdateType.Add);

        Assert.AreEqual("customType", ev.Type);
        Assert.AreEqual(Id, ev.Id);
    }

    [TestMethod]
    public void ProtectEventUpdateType_Values()
    {
        Assert.AreEqual(0, (int)ProtectEventUpdateType.Add);
        Assert.AreEqual(1, (int)ProtectEventUpdateType.Update);
    }
}
