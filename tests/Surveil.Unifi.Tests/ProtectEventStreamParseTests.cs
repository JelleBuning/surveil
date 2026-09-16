using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Surveil.Unifi.Tests;

[TestClass]
public sealed class ProtectEventStreamParseTests
{
    private static string EventJson(
        string type,
        string updateType = "add",
        string? end = null,
        string? smartDetectTypes = null,
        string? metadata = null)
    {
        var optionalFields = string.Empty;
        if (end is not null) optionalFields += ""","end":""" + end;
        if (smartDetectTypes is not null) optionalFields += ""","smartDetectTypes":""" + smartDetectTypes;
        if (metadata is not null) optionalFields += ""","metadata":""" + metadata;

        return $$$"""{"type":"{{{updateType}}}","item":{"id":"ev1","type":"{{{type}}}","start":1000{{{optionalFields}}},"device":"dev1"}}""";
    }

    [TestMethod]
    public void ParseEvent_Motion_ReturnsMotionEvent()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("motion"));

        Assert.IsInstanceOfType<MotionEvent>(result, out var ev);
        Assert.AreEqual("ev1", ev.Id);
        Assert.AreEqual(1000L, ev.Start);
        Assert.AreEqual("dev1", ev.DeviceId);
        Assert.AreEqual(ProtectEventUpdateType.Add, ev.UpdateType);
        Assert.IsNull(ev.End);
    }

    [TestMethod]
    public void ParseEvent_MotionWithEnd_HasEndTimestamp()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("motion", updateType: "update", end: "2000"));

        Assert.IsInstanceOfType<MotionEvent>(result, out var ev);
        Assert.AreEqual(2000L, ev.End);
        Assert.AreEqual(ProtectEventUpdateType.Update, ev.UpdateType);
    }

    [TestMethod]
    public void ParseEvent_NullEndProperty_ReturnsNullEnd()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("motion", end: "null"));

        Assert.IsInstanceOfType<MotionEvent>(result, out var ev);
        Assert.IsNull(ev.End);
    }

    [TestMethod]
    public void ParseEvent_SmartDetectZone_ReturnsWithSmartTypes()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("smartDetectZone", smartDetectTypes: """["person","vehicle"]"""));

        Assert.IsInstanceOfType<SmartDetectZoneEvent>(result, out var ev);
        CollectionAssert.AreEqual(new[] { "person", "vehicle" }, ev.SmartDetectTypes.ToArray());
    }

    [DataTestMethod]
    [DataRow("smartDetectLine", """["car"]""", typeof(SmartDetectLineEvent))]
    [DataRow("smartDetectLoiterZone", "[]", typeof(SmartDetectLoiterZoneEvent))]
    [DataRow("smartAudioDetect", """["smoke"]""", typeof(SmartAudioDetectEvent))]
    [DataRow("ring", null, typeof(RingEvent))]
    [DataRow("lightMotion", null, typeof(LightMotionEvent))]
    [DataRow("sensorMotion", null, typeof(SensorMotionEvent))]
    [DataRow("sensorTamper", null, typeof(SensorTamperEvent))]
    [DataRow("sensorSmokeTest", null, typeof(SensorSmokeTestEvent))]
    public void ParseEvent_KnownType_ReturnsMatchingEvent(string type, string? smartDetectTypes, Type expected)
    {
        var result = ProtectEventStream.ParseEvent(EventJson(type, smartDetectTypes: smartDetectTypes));

        Assert.IsInstanceOfType(result, expected);
    }

    [TestMethod]
    public void ParseEvent_SensorAlarm_WithMetadata_ReturnsAlarmType()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("sensorAlarm", metadata: """{"alarmType":{"text":"smoke"}}"""));

        Assert.IsInstanceOfType<SensorAlarmEvent>(result, out var ev);
        Assert.AreEqual("smoke", ev.AlarmType);
    }

    [TestMethod]
    public void ParseEvent_SensorAlarm_WithoutMetadata_ReturnsEmptyAlarmType()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorAlarm"));

        Assert.IsInstanceOfType<SensorAlarmEvent>(result, out var ev);
        Assert.AreEqual(string.Empty, ev.AlarmType);
    }

    [TestMethod]
    public void ParseEvent_SensorOpened_WithMountType_ReturnsMountType()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("sensorOpened", metadata: """{"sensorMountType":{"text":"door"}}"""));

        Assert.IsInstanceOfType<SensorOpenedEvent>(result, out var ev);
        Assert.AreEqual("door", ev.MountType);
    }

    [TestMethod]
    public void ParseEvent_SensorOpened_WithoutMetadata_ReturnsEmptyMountType()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorOpened"));

        Assert.IsInstanceOfType<SensorOpenedEvent>(result, out var ev);
        Assert.AreEqual(string.Empty, ev.MountType);
    }

    [TestMethod]
    public void ParseEvent_SensorClosed_WithMountType_ReturnsMountType()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("sensorClosed", metadata: """{"sensorMountType":{"text":"window"}}"""));

        Assert.IsInstanceOfType<SensorClosedEvent>(result, out var ev);
        Assert.AreEqual("window", ev.MountType);
    }

    [TestMethod]
    public void ParseEvent_SensorClosed_WithoutMetadata_ReturnsEmptyMountType()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorClosed"));

        Assert.IsInstanceOfType<SensorClosedEvent>(result, out var ev);
        Assert.AreEqual(string.Empty, ev.MountType);
    }

    [TestMethod]
    public void ParseEvent_SensorWaterLeak_WithMountType_ReturnsMountType()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("sensorWaterLeak", metadata: """{"sensorMountType":{"text":"leak"}}"""));

        Assert.IsInstanceOfType<SensorWaterLeakEvent>(result, out var ev);
        Assert.AreEqual("leak", ev.MountType);
    }

    [TestMethod]
    public void ParseEvent_SensorWaterLeak_WithoutMetadata_ReturnsEmptyMountType()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorWaterLeak"));

        Assert.IsInstanceOfType<SensorWaterLeakEvent>(result, out var ev);
        Assert.AreEqual(string.Empty, ev.MountType);
    }

    [TestMethod]
    public void ParseEvent_SensorBatteryLow_WithPercentage_ReturnsBatteryPercentage()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("sensorBatteryLow", metadata: """{"sensorBatteryPercentage":{"number":12.5}}"""));

        Assert.IsInstanceOfType<SensorBatteryLowEvent>(result, out var ev);
        Assert.AreEqual(12.5, ev.BatteryPercentage);
    }

    [TestMethod]
    public void ParseEvent_SensorBatteryLow_WithoutMetadata_ReturnsZeroPercentage()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorBatteryLow"));

        Assert.IsInstanceOfType<SensorBatteryLowEvent>(result, out var ev);
        Assert.AreEqual(0d, ev.BatteryPercentage);
    }

    [TestMethod]
    public void ParseEvent_SensorExtremeValues_WithAllMetadata_ReturnsAllFields()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorExtremeValues",
            metadata: """{"sensorType":{"text":"temperature"},"sensorValue":{"text":42.5},"status":{"text":"high"}}"""));

        Assert.IsInstanceOfType<SensorExtremeValuesEvent>(result, out var ev);
        Assert.AreEqual("temperature", ev.SensorType);
        Assert.AreEqual(42.5, ev.SensorValue);
        Assert.AreEqual("high", ev.Status);
    }

    [TestMethod]
    public void ParseEvent_SensorExtremeValues_WithoutMetadata_ReturnsDefaults()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorExtremeValues"));

        Assert.IsInstanceOfType<SensorExtremeValuesEvent>(result, out var ev);
        Assert.AreEqual(string.Empty, ev.SensorType);
        Assert.AreEqual(0d, ev.SensorValue);
        Assert.AreEqual(string.Empty, ev.Status);
    }

    [TestMethod]
    public void ParseEvent_UnknownType_ReturnsUnknownEvent()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("mystery"));

        Assert.IsInstanceOfType<UnknownEvent>(result, out var ev);
        Assert.AreEqual("mystery", ev.Type);
    }

    [TestMethod]
    public void ParseEvent_InvalidJson_ReturnsNull()
    {
        Assert.IsNull(ProtectEventStream.ParseEvent("not-valid-json"));
    }

    [TestMethod]
    public void ParseEvent_MissingItemProperty_ReturnsNull()
    {
        Assert.IsNull(ProtectEventStream.ParseEvent("""{"type":"add"}"""));
    }
}
