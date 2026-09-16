namespace Surveil.Unifi;

public sealed class EventNotificationSettings
{
    public bool Motion { get; init; }
    public bool SmartDetectZone { get; init; }
    public bool SmartDetectLine { get; init; }
    public bool SmartDetectLoiterZone { get; init; }
    public bool SmartAudioDetect { get; init; }

    public bool Ring { get; init; } = true;

    public bool LightMotion { get; init; }

    public bool SensorMotion { get; init; }
    public bool SensorTamper { get; init; }
    public bool SensorSmokeTest { get; init; }
    public bool SensorAlarm { get; init; }
    public bool SensorOpened { get; init; }
    public bool SensorClosed { get; init; }
    public bool SensorWaterLeak { get; init; }
    public bool SensorBatteryLow { get; init; }
    public bool SensorExtremeValues { get; init; }

    public bool IsEnabled(ProtectEvent @event) => @event switch
    {
        MotionEvent => Motion,
        SmartDetectZoneEvent => SmartDetectZone,
        SmartDetectLineEvent => SmartDetectLine,
        SmartDetectLoiterZoneEvent => SmartDetectLoiterZone,
        SmartAudioDetectEvent => SmartAudioDetect,
        RingEvent => Ring,
        LightMotionEvent => LightMotion,
        SensorMotionEvent => SensorMotion,
        SensorTamperEvent => SensorTamper,
        SensorSmokeTestEvent => SensorSmokeTest,
        SensorAlarmEvent => SensorAlarm,
        SensorOpenedEvent => SensorOpened,
        SensorClosedEvent => SensorClosed,
        SensorWaterLeakEvent => SensorWaterLeak,
        SensorBatteryLowEvent => SensorBatteryLow,
        SensorExtremeValuesEvent => SensorExtremeValues,
        _ => false
    };
}
