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
    private readonly Lock _optionsLock = new();
    private UnifiProtectOptions _options;
    private readonly IWebSocketFactory _wsFactory;
    private readonly EventNotificationSettings _eventSettings;

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

    private void OnSettingsChanged(AppSettings settings)
    {
        lock (_optionsLock)
        {
            _options = new UnifiProtectOptions
            {
                BaseUrl = settings.UnifiProtect.BaseUrl,
                ApiKey  = settings.UnifiProtect.ApiKey
            };
        }

        var oldCts = Interlocked.Exchange(ref _reconnectCts, new CancellationTokenSource());
        oldCts.Cancel();
        oldCts.Dispose();
    }

    private UnifiProtectOptions CurrentOptions
    {
        get { lock (_optionsLock) return _options; }
    }

    public async IAsyncEnumerable<CameraEvent> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var backoffMs = 1_000;

        while (!ct.IsCancellationRequested)
        {
            var wsUri = BuildWebSocketUri();
            Debug.WriteLine($"[ProtectEventStream] Connecting to {wsUri}");

            var reconnectToken = Volatile.Read(ref _reconnectCts).Token;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, reconnectToken);
            var token = linkedCts.Token;

            using var ws = _wsFactory.Create(CurrentOptions.ApiKey);

            var connected = false;
            var cancelled = false;

            try
            {
                await ws.ConnectAsync(wsUri, token);
                connected = true;
                backoffMs = 1_000;
                Debug.WriteLine($"[ProtectEventStream] Connected to {wsUri}");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                cancelled = true;
            }
            catch (OperationCanceledException)
            {
                // Settings changed mid-connect — loop again immediately with fresh options.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProtectEventStream] Connect failed ({ex.GetType().Name}): {ex.Message} — URI: {wsUri}");
            }

            if (cancelled) yield break;

            if (connected)
            {
                await foreach (var ev in ReceiveAsync(ws, token, ct))
                {
                    if (ShouldEmit(ev))
                        yield return ToCameraEvent(ev);
                }

                Debug.WriteLine($"[ProtectEventStream] Disconnected (ws state: {ws.State}), retrying in {backoffMs}ms");
            }

            if (ct.IsCancellationRequested) yield break;

            var delayCancelled = false;
            try { await Task.Delay(backoffMs, token); }
            catch (OperationCanceledException) { delayCancelled = true; }

            if (ct.IsCancellationRequested) yield break;
            if (delayCancelled) continue; // reconnect requested — retry now, don't grow the backoff

            backoffMs = Math.Min(backoffMs * 2, 30_000);
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
        var sb = new StringBuilder();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            sb.Clear();
            var closed = false;
            var interrupted = false;
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
                    // Settings changed — end this receive loop so the caller can reconnect.
                    interrupted = true;
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close) { closed = true; break; }
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                endOfMessage = result.EndOfMessage;
            }
            while (!endOfMessage);

            if (closed || interrupted) yield break;

            var ev = ParseEvent(sb.ToString());
            if (ev is not null)
                yield return ev;
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

            var item   = root.GetProperty("item");
            var id     = item.GetProperty("id").GetString()     ?? string.Empty;
            var type   = item.GetProperty("type").GetString()   ?? string.Empty;
            var start  = item.GetProperty("start").GetInt64();
            var device = item.GetProperty("device").GetString() ?? string.Empty;

            Debug.WriteLine($"[ProtectEventStream] Event: {updateType} {type} device={device}");

            long? end = item.TryGetProperty("end", out var endProp) && endProp.ValueKind != JsonValueKind.Null
                ? endProp.GetInt64()
                : null;

            var smartTypes = item.TryGetProperty("smartDetectTypes", out var stProp)
                             && stProp.ValueKind == JsonValueKind.Array
                ? stProp.EnumerateArray()
                         .Select(e => e.GetString() ?? string.Empty)
                         .ToList()
                         .AsReadOnly()
                : (IReadOnlyList<string>)[];

            return type switch
            {
                // Camera
                "motion"                => new MotionEvent(id, start, end, device, updateType),
                "smartDetectZone"       => new SmartDetectZoneEvent(id, start, end, device, updateType, smartTypes),
                "smartDetectLine"       => new SmartDetectLineEvent(id, start, end, device, updateType, smartTypes),
                "smartDetectLoiterZone" => new SmartDetectLoiterZoneEvent(id, start, end, device, updateType, smartTypes),
                "smartAudioDetect"      => new SmartAudioDetectEvent(id, start, end, device, updateType, smartTypes),

                // Doorbell
                "ring"                  => new RingEvent(id, start, end, device, updateType),

                // Floodlight
                "lightMotion"           => new LightMotionEvent(id, start, device, updateType),

                // Sensors — simple (no relevant metadata)
                "sensorMotion"          => new SensorMotionEvent(id, start, end, device, updateType),
                "sensorTamper"          => new SensorTamperEvent(id, start, end, device, updateType),
                "sensorSmokeTest"       => new SensorSmokeTestEvent(id, start, end, device, updateType),

                // Sensors — with metadata
                "sensorAlarm"           => new SensorAlarmEvent(id, start, end, device, updateType,
                                              MetadataText(item, "alarmType")),
                "sensorOpened"          => new SensorOpenedEvent(id, start, end, device, updateType,
                                              MetadataText(item, "sensorMountType")),
                "sensorClosed"          => new SensorClosedEvent(id, start, end, device, updateType,
                                              MetadataText(item, "sensorMountType")),
                "sensorWaterLeak"       => new SensorWaterLeakEvent(id, start, end, device, updateType,
                                              MetadataText(item, "sensorMountType")),
                "sensorBatteryLow"      => new SensorBatteryLowEvent(id, start, end, device, updateType,
                                              MetadataNumber(item, "sensorBatteryPercentage", "number")),
                "sensorExtremeValues"   => new SensorExtremeValuesEvent(id, start, end, device, updateType,
                                              MetadataText(item, "sensorType"),
                                              MetadataNumber(item, "sensorValue", "text"),
                                              MetadataText(item, "status")),

                _                       => new UnknownEvent(id, type, start, end, device, updateType)
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProtectEventStream] Parse error: {ex.Message}");
            Debug.WriteLine($"[ProtectEventStream] Raw JSON: {json[..Math.Min(json.Length, 500)]}");
            return null;
        }
    }

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
