using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Events;
using Surveil.Unifi.WebSocket;

namespace Surveil.Unifi;

public sealed class ProtectEventStream : ICameraEventStream
{
    private const int InitialBackoffMs = 1_000;
    private const int MaxBackoffMs = 30_000;

    private readonly Lock _optionsLock = new();
    private readonly IWebSocketFactory _wsFactory;
    private readonly EventNotificationSettings _eventSettings;

    private UnifiProtectOptions _options;
    private CancellationTokenSource _reconnectCts = new();

    public ProtectEventStream(
        IOptions<UnifiProtectOptions> options,
        IOptions<EventNotificationSettings> eventSettings,
        ISettingsChangeNotifier notifier)
        : this(options, new ClientWebSocketFactory(), eventSettings.Value, notifier) { }

    internal ProtectEventStream(
        IOptions<UnifiProtectOptions> options,
        IWebSocketFactory wsFactory,
        EventNotificationSettings eventSettings,
        ISettingsChangeNotifier? notifier = null)
    {
        _options = options.Value;
        _eventSettings = eventSettings;
        _wsFactory = wsFactory;

        if (notifier is not null)
            notifier.SettingsChanged += OnSettingsChanged;
    }

    private UnifiProtectOptions CurrentOptions
    {
        get { lock (_optionsLock) return _options; }
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        var newOptions = new UnifiProtectOptions
        {
            BaseUrl = settings.UnifiProtect.BaseUrl,
            ApiKey = settings.UnifiProtect.ApiKey
        };

        bool changed;
        lock (_optionsLock)
        {
            changed = newOptions.BaseUrl != _options.BaseUrl || newOptions.ApiKey != _options.ApiKey;
            _options = newOptions;
        }

        if (!changed) return;

        var previousCts = Interlocked.Exchange(ref _reconnectCts, new CancellationTokenSource());
        previousCts.Cancel();
        previousCts.Dispose();
    }

    public async IAsyncEnumerable<CameraEvent> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var backoffMs = InitialBackoffMs;

        while (!ct.IsCancellationRequested)
        {
            var wsUri = BuildWebSocketUri();
            Debug.WriteLine($"[ProtectEventStream] Connecting to {wsUri}");

            var reconnectToken = Volatile.Read(ref _reconnectCts).Token;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, reconnectToken);
            var token = linkedCts.Token;

            using var ws = _wsFactory.Create(CurrentOptions.ApiKey);

            var connected = false;
            var stopping = false;

            try
            {
                await ws.ConnectAsync(wsUri, token);
                connected = true;
                backoffMs = InitialBackoffMs;
                Debug.WriteLine($"[ProtectEventStream] Connected to {wsUri}");
            }
            catch (OperationCanceledException)
            {
                stopping = ct.IsCancellationRequested;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProtectEventStream] Connect failed ({ex.GetType().Name}): {ex.Message} — URI: {wsUri}");
            }

            if (stopping) yield break;

            if (connected)
            {
                await foreach (var protectEvent in ReceiveAsync(ws, token, ct))
                {
                    if (ShouldEmit(protectEvent))
                        yield return ToCameraEvent(protectEvent);
                }

                Debug.WriteLine($"[ProtectEventStream] Disconnected (ws state: {ws.State}), retrying in {backoffMs}ms");
            }

            if (ct.IsCancellationRequested) yield break;

            var reconnectRequested = false;
            try { await Task.Delay(backoffMs, token); }
            catch (OperationCanceledException) { reconnectRequested = true; }

            if (ct.IsCancellationRequested) yield break;
            if (reconnectRequested) continue;

            backoffMs = Math.Min(backoffMs * 2, MaxBackoffMs);
        }
    }

    private bool ShouldEmit(ProtectEvent @event) =>
        IsNotifiable(@event) && _eventSettings.IsEnabled(@event);

    internal static bool IsNotifiable(ProtectEvent @event) =>
        @event.UpdateType == ProtectEventUpdateType.Add || @event is RingEvent { End: null };

    internal static CameraEvent ToCameraEvent(ProtectEvent @event) =>
        new(@event.Id, @event.DeviceId, ProtectEventDescriber.Describe(@event));

    internal Uri BuildWebSocketUri()
    {
        var baseUri = new Uri($"{CurrentOptions.BaseUrl.TrimEnd('/')}/{UnifiProtectOptions.ApiPath}/");
        var builder = new UriBuilder(baseUri)
        {
            Scheme = baseUri.Scheme == "https" ? "wss" : "ws"
        };
        return new Uri(builder.Uri, "v1/subscribe/events");
    }

    private static async IAsyncEnumerable<ProtectEvent> ReceiveAsync(
        IWebSocketConnection ws,
        CancellationToken receiveToken,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        var message = new StringBuilder();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            message.Clear();
            var closedByServer = false;
            var reconnectRequested = false;
            bool endOfMessage;

            do
            {
                ValueWebSocketReceiveResult result;
                try
                {
                    result = await ws.ReceiveAsync(buffer.AsMemory(), receiveToken);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    reconnectRequested = true;
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    closedByServer = true;
                    break;
                }

                message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                endOfMessage = result.EndOfMessage;
            }
            while (!endOfMessage);

            if (closedByServer || reconnectRequested) yield break;

            var protectEvent = ParseEvent(message.ToString());
            if (protectEvent is not null)
                yield return protectEvent;
        }
    }

    internal static ProtectEvent? ParseEvent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var updateType = root.GetProperty("type").GetString() == "add"
                ? ProtectEventUpdateType.Add
                : ProtectEventUpdateType.Update;

            var item = root.GetProperty("item");
            var id = item.GetProperty("id").GetString() ?? string.Empty;
            var type = item.GetProperty("type").GetString() ?? string.Empty;
            var start = item.GetProperty("start").GetInt64();
            var device = item.GetProperty("device").GetString() ?? string.Empty;

            Debug.WriteLine($"[ProtectEventStream] Event: {updateType} {type} device={device}");

            var end = ReadEnd(item);
            var smartTypes = ReadSmartDetectTypes(item);

            return type switch
            {
                "motion" => new MotionEvent(id, start, end, device, updateType),
                "smartDetectZone" => new SmartDetectZoneEvent(id, start, end, device, updateType, smartTypes),
                "smartDetectLine" => new SmartDetectLineEvent(id, start, end, device, updateType, smartTypes),
                "smartDetectLoiterZone" => new SmartDetectLoiterZoneEvent(id, start, end, device, updateType, smartTypes),
                "smartAudioDetect" => new SmartAudioDetectEvent(id, start, end, device, updateType, smartTypes),

                "ring" => new RingEvent(id, start, end, device, updateType),

                "lightMotion" => new LightMotionEvent(id, start, device, updateType),

                "sensorMotion" => new SensorMotionEvent(id, start, end, device, updateType),
                "sensorTamper" => new SensorTamperEvent(id, start, end, device, updateType),
                "sensorSmokeTest" => new SensorSmokeTestEvent(id, start, end, device, updateType),

                "sensorAlarm" => new SensorAlarmEvent(id, start, end, device, updateType,
                    MetadataText(item, "alarmType")),
                "sensorOpened" => new SensorOpenedEvent(id, start, end, device, updateType,
                    MetadataText(item, "sensorMountType")),
                "sensorClosed" => new SensorClosedEvent(id, start, end, device, updateType,
                    MetadataText(item, "sensorMountType")),
                "sensorWaterLeak" => new SensorWaterLeakEvent(id, start, end, device, updateType,
                    MetadataText(item, "sensorMountType")),
                "sensorBatteryLow" => new SensorBatteryLowEvent(id, start, end, device, updateType,
                    MetadataNumber(item, "sensorBatteryPercentage", "number")),
                "sensorExtremeValues" => new SensorExtremeValuesEvent(id, start, end, device, updateType,
                    MetadataText(item, "sensorType"),
                    MetadataNumber(item, "sensorValue", "text"),
                    MetadataText(item, "status")),

                _ => new UnknownEvent(id, type, start, end, device, updateType)
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProtectEventStream] Parse error: {ex.Message}");
            Debug.WriteLine($"[ProtectEventStream] Raw JSON: {UnifiProtectApiClient.Truncate(json, 500)}");
            return null;
        }
    }

    private static long? ReadEnd(JsonElement item) =>
        item.TryGetProperty("end", out var end) && end.ValueKind != JsonValueKind.Null
            ? end.GetInt64()
            : null;

    private static IReadOnlyList<string> ReadSmartDetectTypes(JsonElement item) =>
        item.TryGetProperty("smartDetectTypes", out var types) && types.ValueKind == JsonValueKind.Array
            ? types.EnumerateArray().Select(t => t.GetString() ?? string.Empty).ToList().AsReadOnly()
            : [];

    private static string MetadataText(JsonElement item, string field) =>
        TryGetMetadataValue(item, field, "text", out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static double MetadataNumber(JsonElement item, string field, string valueProperty) =>
        TryGetMetadataValue(item, field, valueProperty, out var value)
            ? value.GetDouble()
            : 0d;

    private static bool TryGetMetadataValue(JsonElement item, string field, string valueProperty, out JsonElement value)
    {
        value = default;
        return item.TryGetProperty("metadata", out var metadata)
               && metadata.TryGetProperty(field, out var fieldElement)
               && fieldElement.TryGetProperty(valueProperty, out value);
    }
}
