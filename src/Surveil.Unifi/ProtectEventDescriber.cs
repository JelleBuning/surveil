using System.Collections.Generic;
using System.Linq;

namespace Surveil.Unifi;

internal static class ProtectEventDescriber
{
    internal static string Describe(ProtectEvent protectEvent) => protectEvent switch
    {
        MotionEvent => "Motion detected",
        SmartDetectZoneEvent { SmartDetectTypes.Count: > 0 } e => $"{FormatSmartTypes(e.SmartDetectTypes)} detected",
        SmartDetectZoneEvent => "Smart detection",
        SmartDetectLineEvent { SmartDetectTypes.Count: > 0 } e => $"{FormatSmartTypes(e.SmartDetectTypes)} crossed line",
        SmartDetectLineEvent => "Line crossing",
        SmartDetectLoiterZoneEvent { SmartDetectTypes.Count: > 0 } e => $"{FormatSmartTypes(e.SmartDetectTypes)} loitering",
        SmartDetectLoiterZoneEvent => "Loitering detected",
        SmartAudioDetectEvent { SmartDetectTypes.Count: > 0 } e => $"Audio: {FormatSmartTypes(e.SmartDetectTypes)}",
        SmartAudioDetectEvent => "Audio detection",

        RingEvent => "Doorbell ring",
        LightMotionEvent => "Floodlight motion",

        SensorMotionEvent => "Sensor motion",
        SensorTamperEvent => "Sensor tampered",
        SensorSmokeTestEvent => "Smoke detector test",
        SensorAlarmEvent { AlarmType.Length: > 0 } e => $"Sensor alarm: {e.AlarmType}",
        SensorAlarmEvent => "Sensor alarm",
        SensorOpenedEvent { MountType.Length: > 0 } e => $"{Capitalize(e.MountType)} opened",
        SensorOpenedEvent => "Sensor opened",
        SensorClosedEvent { MountType.Length: > 0 } e => $"{Capitalize(e.MountType)} closed",
        SensorClosedEvent => "Sensor closed",
        SensorWaterLeakEvent => "Water leak detected",
        SensorBatteryLowEvent e => $"Low battery: {e.BatteryPercentage:F0}%",
        SensorExtremeValuesEvent e => $"{Capitalize(e.SensorType)} {e.Status}: {e.SensorValue:F1}",

        _ => $"Event: {protectEvent.Type}"
    };

    internal static string FormatSmartTypes(IReadOnlyList<string> types) =>
        string.Join(", ", types.Where(t => t.Length > 0).Select(Capitalize));

    internal static string Capitalize(string? input) =>
        string.IsNullOrEmpty(input) ? string.Empty : char.ToUpper(input[0]) + input[1..];
}
