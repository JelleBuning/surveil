using System;
using System.Threading;
using System.Threading.Tasks;

namespace Surveil.Services;

public sealed class RtspVideoPlayer : IDisposable
{
    private readonly string _url;
    private readonly IVlcPlayerFactory _factory;
    private readonly Lock _handleLock = new();
    private IVlcPlayerHandle? _handle;
    private CancellationTokenSource _cts = new();
    private bool _stopped;
    private bool _disposed;
    private int _reconnectPending; // 0 = idle, 1 = reconnect scheduled (Interlocked)
    private DateTime _connectingStart;

    public event EventHandler<VideoFrame>? FrameReady;
    public event EventHandler<string>? StatusChanged;

    public RtspVideoPlayer(string url)
        : this(url, new DefaultVlcPlayerFactory()) { }

    internal RtspVideoPlayer(string url, IVlcPlayerFactory factory)
    {
        _url = url;
        _factory = factory;
    }

    public void Start()
    {
        lock (_handleLock)
        {
            if (_disposed) return;

            TearDownPlayerLocked();
            _stopped = false;
        }

        Interlocked.Exchange(ref _reconnectPending, 0);
        _cts = new CancellationTokenSource();
        StartInternal();
    }

    internal void StartInternal()
    {
        lock (_handleLock)
        {
            if (_stopped || _disposed) return;

            _connectingStart = DateTime.UtcNow;
            StatusChanged?.Invoke(this, "Connecting...");

            var handle = _factory.Create(_url, msg => StatusChanged?.Invoke(this, msg));
            handle.Playing += OnHandlePlaying;
            handle.EncounteredError += (_, _) => ScheduleReconnect("Playback error");
            handle.EndReached += (_, _) => ScheduleReconnect("Stream ended");
            handle.FrameReady += (_, frame) => FrameReady?.Invoke(this, frame);

            _handle = handle;
            handle.Play();
        }
    }

    private void OnHandlePlaying(object? sender, EventArgs e)
    {
        var token = _cts.Token;
        Task.Run(async () =>
        {
            try
            {
                var elapsed = DateTime.UtcNow - _connectingStart;
                var remaining = TimeSpan.FromMilliseconds(1500) - elapsed;
                if (remaining > TimeSpan.Zero)
                    await Task.Delay(remaining, token);
                StatusChanged?.Invoke(this, "Connected");
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    internal void ScheduleReconnect(string reason)
    {
        if (_stopped) return;

        // Guard: only one reconnect task at a time (EncounteredError + EndReached can both fire)
        if (Interlocked.CompareExchange(ref _reconnectPending, 1, 0) != 0) return;

        StatusChanged?.Invoke(this, reason);

        var token = _cts.Token;
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), token);
                TearDownPlayer();
                Interlocked.Exchange(ref _reconnectPending, 0);
                StartInternal();
            }
            catch (OperationCanceledException)
            {
                Interlocked.Exchange(ref _reconnectPending, 0);
            }
        }, token);
    }

    public void Stop()
    {
        _stopped = true;
        _cts.Cancel();
        TearDownPlayer();
    }

    internal void TearDownPlayer()
    {
        lock (_handleLock)
        {
            TearDownPlayerLocked();
        }
    }

    private void TearDownPlayerLocked()
    {
        _handle?.Dispose();
        _handle = null;
    }

    public void Dispose()
    {
        lock (_handleLock)
        {
            _disposed = true;
        }

        Stop();
    }
}
