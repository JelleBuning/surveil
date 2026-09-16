using System.Collections.Generic;

namespace Surveil.Unifi;

public enum ProtectEventUpdateType { Add, Update }

public abstract record ProtectEvent(
    string Id,
    string Type,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType);

public sealed record MotionEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType)
    : ProtectEvent(Id, "motion", Start, End, DeviceId, UpdateType);

public sealed record SmartDetectZoneEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType,
    IReadOnlyList<string> SmartDetectTypes)
    : ProtectEvent(Id, "smartDetectZone", Start, End, DeviceId, UpdateType);

public sealed record SmartDetectLineEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType,
    IReadOnlyList<string> SmartDetectTypes)
    : ProtectEvent(Id, "smartDetectLine", Start, End, DeviceId, UpdateType);

public sealed record SmartDetectLoiterZoneEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType,
    IReadOnlyList<string> SmartDetectTypes)
    : ProtectEvent(Id, "smartDetectLoiterZone", Start, End, DeviceId, UpdateType);

public sealed record SmartAudioDetectEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType,
    IReadOnlyList<string> SmartDetectTypes)
    : ProtectEvent(Id, "smartAudioDetect", Start, End, DeviceId, UpdateType);

public sealed record RingEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType)
    : ProtectEvent(Id, "ring", Start, End, DeviceId, UpdateType);

public sealed record LightMotionEvent(
    string Id,
    long Start,
    string DeviceId,
    ProtectEventUpdateType UpdateType)
    : ProtectEvent(Id, "lightMotion", Start, End: null, DeviceId, UpdateType);

public sealed record SensorMotionEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType)
    : ProtectEvent(Id, "sensorMotion", Start, End, DeviceId, UpdateType);

public sealed record SensorTamperEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType)
    : ProtectEvent(Id, "sensorTamper", Start, End, DeviceId, UpdateType);

public sealed record SensorSmokeTestEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType)
    : ProtectEvent(Id, "sensorSmokeTest", Start, End, DeviceId, UpdateType);

public sealed record SensorAlarmEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType,
    string AlarmType)
    : ProtectEvent(Id, "sensorAlarm", Start, End, DeviceId, UpdateType);

public sealed record SensorOpenedEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType,
    string MountType)
    : ProtectEvent(Id, "sensorOpened", Start, End, DeviceId, UpdateType);

public sealed record SensorClosedEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType,
    string MountType)
    : ProtectEvent(Id, "sensorClosed", Start, End, DeviceId, UpdateType);

public sealed record SensorWaterLeakEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType,
    string MountType)
    : ProtectEvent(Id, "sensorWaterLeak", Start, End, DeviceId, UpdateType);

public sealed record SensorBatteryLowEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType,
    double BatteryPercentage)
    : ProtectEvent(Id, "sensorBatteryLow", Start, End, DeviceId, UpdateType);

public sealed record SensorExtremeValuesEvent(
    string Id,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType,
    string SensorType,
    double SensorValue,
    string Status)
    : ProtectEvent(Id, "sensorExtremeValues", Start, End, DeviceId, UpdateType);

public sealed record UnknownEvent(
    string Id,
    string Type,
    long Start,
    long? End,
    string DeviceId,
    ProtectEventUpdateType UpdateType)
    : ProtectEvent(Id, Type, Start, End, DeviceId, UpdateType);
