using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Surveil.Unifi.Tests;

[TestClass]
public sealed class ProtectEventDescriberTests
{
    private static string Title(ProtectEvent protectEvent, string cameraName) =>
        $"{ProtectEventStream.ToCameraEvent(protectEvent).Description} ({cameraName})";

    [TestMethod]
    public void Title_MotionEvent_ContainsCameraName()
    {
        var ev = new MotionEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.AreEqual("Motion detected (Front Door)", Title(ev, "Front Door"));
    }

    [TestMethod]
    public void Title_SmartDetectZoneWithTypes_FormatsTypes()
    {
        var ev = new SmartDetectZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, ["person", "vehicle"]);

        Assert.AreEqual("Person, Vehicle detected (Backyard)", Title(ev, "Backyard"));
    }

    [TestMethod]
    public void Title_SmartDetectZoneWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartDetectZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, []);

        Assert.AreEqual("Smart detection (Backyard)", Title(ev, "Backyard"));
    }

    [TestMethod]
    public void Title_SmartDetectLineWithTypes_FormatsLineMessage()
    {
        var ev = new SmartDetectLineEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, ["car"]);

        Assert.AreEqual("Car crossed line (Gate)", Title(ev, "Gate"));
    }

    [TestMethod]
    public void Title_SmartDetectLineWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartDetectLineEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, []);

        Assert.AreEqual("Line crossing (Gate)", Title(ev, "Gate"));
    }

    [TestMethod]
    public void Title_SmartDetectLoiterZoneWithTypes_FormatsLoiteringMessage()
    {
        var ev = new SmartDetectLoiterZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, ["person"]);

        Assert.AreEqual("Person loitering (Park)", Title(ev, "Park"));
    }

    [TestMethod]
    public void Title_SmartDetectLoiterZoneWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartDetectLoiterZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, []);

        Assert.AreEqual("Loitering detected (Park)", Title(ev, "Park"));
    }

    [TestMethod]
    public void Title_SmartAudioDetectWithTypes_FormatsAudioMessage()
    {
        var ev = new SmartAudioDetectEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, ["smoke"]);

        Assert.AreEqual("Audio: Smoke (Kitchen)", Title(ev, "Kitchen"));
    }

    [TestMethod]
    public void Title_SmartAudioDetectWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartAudioDetectEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, []);

        Assert.AreEqual("Audio detection (Kitchen)", Title(ev, "Kitchen"));
    }

    [TestMethod]
    public void Title_RingEvent_ShowsDoorbellMessage()
    {
        var ev = new RingEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.AreEqual("Doorbell ring (Front Door)", Title(ev, "Front Door"));
    }

    [TestMethod]
    public void Title_LightMotionEvent_ShowsFloodlightMessage()
    {
        var ev = new LightMotionEvent("id", 0, "dev", ProtectEventUpdateType.Add);

        Assert.AreEqual("Floodlight motion (Porch)", Title(ev, "Porch"));
    }

    [TestMethod]
    public void Title_SensorMotionEvent_ShowsSensorMotion()
    {
        var ev = new SensorMotionEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.AreEqual("Sensor motion (Hallway)", Title(ev, "Hallway"));
    }

    [TestMethod]
    public void Title_SensorTamperEvent_ShowsSensorTampered()
    {
        var ev = new SensorTamperEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.AreEqual("Sensor tampered (Garage)", Title(ev, "Garage"));
    }

    [TestMethod]
    public void Title_SensorSmokeTestEvent_ShowsSmokeTest()
    {
        var ev = new SensorSmokeTestEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.AreEqual("Smoke detector test (Living Room)", Title(ev, "Living Room"));
    }

    [TestMethod]
    public void Title_SensorAlarmWithAlarmType_ShowsAlarmType()
    {
        var ev = new SensorAlarmEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "CO");

        Assert.AreEqual("Sensor alarm: CO (Bedroom)", Title(ev, "Bedroom"));
    }

    [TestMethod]
    public void Title_SensorAlarmWithoutAlarmType_ShowsGenericAlarm()
    {
        var ev = new SensorAlarmEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "");

        Assert.AreEqual("Sensor alarm (Bedroom)", Title(ev, "Bedroom"));
    }

    [TestMethod]
    public void Title_SensorOpenedWithMountType_CapitalizesMountType()
    {
        var ev = new SensorOpenedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "door");

        Assert.AreEqual("Door opened (Entry)", Title(ev, "Entry"));
    }

    [TestMethod]
    public void Title_SensorOpenedWithoutMountType_ShowsGenericOpened()
    {
        var ev = new SensorOpenedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "");

        Assert.AreEqual("Sensor opened (Entry)", Title(ev, "Entry"));
    }

    [TestMethod]
    public void Title_SensorClosedWithMountType_CapitalizesMountType()
    {
        var ev = new SensorClosedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "window");

        Assert.AreEqual("Window closed (Office)", Title(ev, "Office"));
    }

    [TestMethod]
    public void Title_SensorClosedWithoutMountType_ShowsGenericClosed()
    {
        var ev = new SensorClosedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "");

        Assert.AreEqual("Sensor closed (Office)", Title(ev, "Office"));
    }

    [TestMethod]
    public void Title_SensorWaterLeak_ShowsWaterLeakMessage()
    {
        var ev = new SensorWaterLeakEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "leak");

        Assert.AreEqual("Water leak detected (Basement)", Title(ev, "Basement"));
    }

    [TestMethod]
    public void Title_SensorBatteryLow_ShowsBatteryPercentage()
    {
        var ev = new SensorBatteryLowEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, 8.0);

        Assert.AreEqual("Low battery: 8% (Door Sensor)", Title(ev, "Door Sensor"));
    }

    [TestMethod]
    public void Title_SensorExtremeValues_FormatsTypeStatusAndValueWithoutAssumingDecimalSeparator()
    {
        var ev = new SensorExtremeValuesEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "temperature", 38.5, "high");

        var title = Title(ev, "Attic Sensor");

        Assert.IsTrue(title.StartsWith("Temperature high:"), $"Title was: {title}");
        Assert.IsTrue(title.EndsWith("(Attic Sensor)"), $"Title was: {title}");
        Assert.Contains("38", title);
    }

    [TestMethod]
    public void Title_UnknownEvent_ShowsEventType()
    {
        var ev = new UnknownEvent("id", "customEvent", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.AreEqual("Event: customEvent (Camera)", Title(ev, "Camera"));
    }

    [DataTestMethod]
    [DataRow(new[] { "person" }, "Person")]
    [DataRow(new[] { "person", "vehicle" }, "Person, Vehicle")]
    [DataRow(new[] { "", "car" }, "Car")]
    [DataRow(new string[] { }, "")]
    public void FormatSmartTypes_CapitalizesAndJoinsNonEmptyTypes(string[] types, string expected)
    {
        Assert.AreEqual(expected, ProtectEventDescriber.FormatSmartTypes(types));
    }

    [DataTestMethod]
    [DataRow(null, "")]
    [DataRow("", "")]
    [DataRow("door", "Door")]
    [DataRow("Window", "Window")]
    [DataRow("a", "A")]
    public void Capitalize_UppercasesFirstCharacterOnly(string? input, string expected)
    {
        Assert.AreEqual(expected, ProtectEventDescriber.Capitalize(input));
    }
}
