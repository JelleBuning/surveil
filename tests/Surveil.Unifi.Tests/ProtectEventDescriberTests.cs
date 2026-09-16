using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Surveil.Unifi.Tests;

[TestClass]
public sealed class ProtectEventDescriberTests
{
    private static string Title(ProtectEvent protectEvent, string cameraName) =>
        $"{ProtectEventStream.ToCameraEvent(protectEvent).Description} ({cameraName})";

    // ── BuildTitle ────────────────────────────────────────────────────────────

    [TestMethod]
    public void BuildTitle_MotionEvent_ContainsCameraName()
    {
        var ev = new MotionEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);
        var title = Title(ev, "Front Door");
        Assert.AreEqual("Motion detected (Front Door)", title);
    }

    [TestMethod]
    public void BuildTitle_SmartDetectZoneWithTypes_FormatsTypes()
    {
        var ev = new SmartDetectZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add,
            new List<string> { "person", "vehicle" }.AsReadOnly());
        var title = Title(ev, "Backyard");
        Assert.AreEqual("Person, Vehicle detected (Backyard)", title);
    }

    [TestMethod]
    public void BuildTitle_SmartDetectZoneWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartDetectZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add,
            new List<string>().AsReadOnly());
        var title = Title(ev, "Backyard");
        Assert.AreEqual("Smart detection (Backyard)", title);
    }

    [TestMethod]
    public void BuildTitle_SmartDetectLineWithTypes_FormatsLineMessage()
    {
        var ev = new SmartDetectLineEvent("id", 0, null, "dev", ProtectEventUpdateType.Add,
            new List<string> { "car" }.AsReadOnly());
        var title = Title(ev, "Gate");
        Assert.AreEqual("Car crossed line (Gate)", title);
    }

    [TestMethod]
    public void BuildTitle_SmartDetectLineWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartDetectLineEvent("id", 0, null, "dev", ProtectEventUpdateType.Add,
            new List<string>().AsReadOnly());
        var title = Title(ev, "Gate");
        Assert.AreEqual("Line crossing (Gate)", title);
    }

    [TestMethod]
    public void BuildTitle_SmartDetectLoiterZoneWithTypes_FormatsLoiteringMessage()
    {
        var ev = new SmartDetectLoiterZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add,
            new List<string> { "person" }.AsReadOnly());
        var title = Title(ev, "Park");
        Assert.AreEqual("Person loitering (Park)", title);
    }

    [TestMethod]
    public void BuildTitle_SmartDetectLoiterZoneWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartDetectLoiterZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add,
            new List<string>().AsReadOnly());
        var title = Title(ev, "Park");
        Assert.AreEqual("Loitering detected (Park)", title);
    }

    [TestMethod]
    public void BuildTitle_SmartAudioDetectWithTypes_FormatsAudioMessage()
    {
        var ev = new SmartAudioDetectEvent("id", 0, null, "dev", ProtectEventUpdateType.Add,
            new List<string> { "smoke" }.AsReadOnly());
        var title = Title(ev, "Kitchen");
        Assert.AreEqual("Audio: Smoke (Kitchen)", title);
    }

    [TestMethod]
    public void BuildTitle_SmartAudioDetectWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartAudioDetectEvent("id", 0, null, "dev", ProtectEventUpdateType.Add,
            new List<string>().AsReadOnly());
        var title = Title(ev, "Kitchen");
        Assert.AreEqual("Audio detection (Kitchen)", title);
    }

    [TestMethod]
    public void BuildTitle_RingEvent_ShowsDoorbellMessage()
    {
        var ev = new RingEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);
        var title = Title(ev, "Front Door");
        Assert.AreEqual("Doorbell ring (Front Door)", title);
    }

    [TestMethod]
    public void BuildTitle_LightMotionEvent_ShowsFloodlightMessage()
    {
        var ev = new LightMotionEvent("id", 0, "dev", ProtectEventUpdateType.Add);
        var title = Title(ev, "Porch");
        Assert.AreEqual("Floodlight motion (Porch)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorMotionEvent_ShowsSensorMotion()
    {
        var ev = new SensorMotionEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);
        var title = Title(ev, "Hallway");
        Assert.AreEqual("Sensor motion (Hallway)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorTamperEvent_ShowsSensorTampered()
    {
        var ev = new SensorTamperEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);
        var title = Title(ev, "Garage");
        Assert.AreEqual("Sensor tampered (Garage)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorSmokeTestEvent_ShowsSmokeTest()
    {
        var ev = new SensorSmokeTestEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);
        var title = Title(ev, "Living Room");
        Assert.AreEqual("Smoke detector test (Living Room)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorAlarmWithAlarmType_ShowsAlarmType()
    {
        var ev = new SensorAlarmEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "CO");
        var title = Title(ev, "Bedroom");
        Assert.AreEqual("Sensor alarm: CO (Bedroom)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorAlarmWithoutAlarmType_ShowsGenericAlarm()
    {
        var ev = new SensorAlarmEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "");
        var title = Title(ev, "Bedroom");
        Assert.AreEqual("Sensor alarm (Bedroom)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorOpenedWithMountType_CapitalizesMountType()
    {
        var ev = new SensorOpenedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "door");
        var title = Title(ev, "Entry");
        Assert.AreEqual("Door opened (Entry)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorOpenedWithoutMountType_ShowsGenericOpened()
    {
        var ev = new SensorOpenedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "");
        var title = Title(ev, "Entry");
        Assert.AreEqual("Sensor opened (Entry)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorClosedWithMountType_CapitalizesMountType()
    {
        var ev = new SensorClosedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "window");
        var title = Title(ev, "Office");
        Assert.AreEqual("Window closed (Office)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorClosedWithoutMountType_ShowsGenericClosed()
    {
        var ev = new SensorClosedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "");
        var title = Title(ev, "Office");
        Assert.AreEqual("Sensor closed (Office)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorWaterLeak_ShowsWaterLeakMessage()
    {
        var ev = new SensorWaterLeakEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "leak");
        var title = Title(ev, "Basement");
        Assert.AreEqual("Water leak detected (Basement)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorBatteryLow_ShowsBatteryPercentage()
    {
        var ev = new SensorBatteryLowEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, 8.0);
        var title = Title(ev, "Door Sensor");
        Assert.AreEqual("Low battery: 8% (Door Sensor)", title);
    }

    [TestMethod]
    public void BuildTitle_SensorExtremeValues_FormatsAllFields()
    {
        var ev = new SensorExtremeValuesEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "temperature", 38.5, "high");
        var title = Title(ev, "Attic Sensor");
        // Format uses F1 — verify structure, not locale-specific decimal separator
        Assert.IsTrue(title.StartsWith("Temperature high:"), $"Title was: {title}");
        Assert.IsTrue(title.EndsWith("(Attic Sensor)"), $"Title was: {title}");
        Assert.IsTrue(title.Contains("38"), $"Title was: {title}");
    }

    [TestMethod]
    public void BuildTitle_UnknownEvent_ShowsEventType()
    {
        var ev = new UnknownEvent("id", "customEvent", 0, null, "dev", ProtectEventUpdateType.Add);
        var title = Title(ev, "Camera");
        Assert.AreEqual("Event: customEvent (Camera)", title);
    }

    // ── FormatSmartTypes ──────────────────────────────────────────────────────

    [TestMethod]
    public void FormatSmartTypes_SingleType_CapitalizesFirst()
    {
        var result = ProtectEventDescriber.FormatSmartTypes(new List<string> { "person" }.AsReadOnly());
        Assert.AreEqual("Person", result);
    }

    [TestMethod]
    public void FormatSmartTypes_MultipleTypes_JoinedWithComma()
    {
        var result = ProtectEventDescriber.FormatSmartTypes(new List<string> { "person", "vehicle" }.AsReadOnly());
        Assert.AreEqual("Person, Vehicle", result);
    }

    [TestMethod]
    public void FormatSmartTypes_EmptyType_SkipsEmpty()
    {
        var result = ProtectEventDescriber.FormatSmartTypes(new List<string> { "", "car" }.AsReadOnly());
        Assert.AreEqual("Car", result);
    }

    [TestMethod]
    public void FormatSmartTypes_EmptyList_ReturnsEmpty()
    {
        var result = ProtectEventDescriber.FormatSmartTypes(new List<string>().AsReadOnly());
        Assert.AreEqual(string.Empty, result);
    }

    // ── Capitalize ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Capitalize_NullInput_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, ProtectEventDescriber.Capitalize(null));
    }

    [TestMethod]
    public void Capitalize_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, ProtectEventDescriber.Capitalize(""));
    }

    [TestMethod]
    public void Capitalize_LowercaseInput_CapitalizesFirstLetter()
    {
        Assert.AreEqual("Door", ProtectEventDescriber.Capitalize("door"));
    }

    [TestMethod]
    public void Capitalize_AlreadyCapitalized_Unchanged()
    {
        Assert.AreEqual("Window", ProtectEventDescriber.Capitalize("Window"));
    }

    [TestMethod]
    public void Capitalize_SingleChar_Capitalizes()
    {
        Assert.AreEqual("A", ProtectEventDescriber.Capitalize("a"));
    }
}
