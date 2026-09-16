using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestClass]
public sealed class RtspVideoPlayerTests
{
    private sealed class TestVlcHandle : IVlcPlayerHandle
    {
        public event EventHandler? Playing;
        public event EventHandler? EncounteredError;
        public event EventHandler? EndReached;
        public event EventHandler<VideoFrame>? FrameReady;

        public int PlayCallCount { get; private set; }
        public int StopCallCount { get; private set; }
        public bool IsDisposed { get; private set; }

        public void Play() => PlayCallCount++;
        public void Stop() => StopCallCount++;
        public void Dispose() => IsDisposed = true;

        public void RaisePlaying() => Playing?.Invoke(this, EventArgs.Empty);
        public void RaiseEncounteredError() => EncounteredError?.Invoke(this, EventArgs.Empty);
        public void RaiseEndReached() => EndReached?.Invoke(this, EventArgs.Empty);
        public void RaiseFrameReady(VideoFrame frame) => FrameReady?.Invoke(this, frame);
    }

    private sealed class TestVlcFactory(TestVlcHandle handle) : IVlcPlayerFactory
    {
        public Action<string>? CapturedOnError { get; private set; }

        public IVlcPlayerHandle Create(string url, Action<string> onError)
        {
            CapturedOnError = onError;
            return handle;
        }
    }

    private readonly TestVlcHandle _handle = new();
    private readonly TestVlcFactory _factory;
    private readonly RtspVideoPlayer _player;

    public RtspVideoPlayerTests()
    {
        _factory = new TestVlcFactory(_handle);
        _player = new RtspVideoPlayer("rtsps://host/stream", _factory);
    }

    private List<string> CaptureStatuses()
    {
        var statuses = new List<string>();
        _player.StatusChanged += (_, status) => statuses.Add(status);
        return statuses;
    }

    [TestMethod]
    public void Start_CallsHandlePlay()
    {
        _player.Start();

        Assert.AreEqual(1, _handle.PlayCallCount);
    }

    [TestMethod]
    public void Start_RaisesStatusChangedConnecting()
    {
        var statuses = CaptureStatuses();

        _player.Start();

        Assert.AreEqual("Connecting...", statuses[^1]);
    }

    [TestMethod]
    public void Start_CalledTwice_DisposesFirstHandleBeforeCreatingNew()
    {
        var firstHandle = new TestVlcHandle();
        var secondHandle = new TestVlcHandle();
        var createCount = 0;
        var factory = new Mock<IVlcPlayerFactory>();
        factory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<Action<string>>()))
               .Returns<string, Action<string>>((_, _) => createCount++ == 0 ? firstHandle : secondHandle);
        var player = new RtspVideoPlayer("rtsps://host", factory.Object);

        player.Start();
        player.Start();

        Assert.IsTrue(firstHandle.IsDisposed);
        Assert.AreEqual(1, secondHandle.PlayCallCount);
    }

    [TestMethod]
    public void Stop_DisposesHandle()
    {
        _player.Start();

        _player.Stop();

        Assert.IsTrue(_handle.IsDisposed);
    }

    [TestMethod]
    public void Stop_WithoutStart_DoesNotThrow()
    {
        _player.Stop();
    }

    [TestMethod]
    public void Dispose_CallsStop()
    {
        _player.Start();

        _player.Dispose();

        Assert.IsTrue(_handle.IsDisposed);
    }

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        _player.Dispose();
        _player.Dispose();
    }

    [TestMethod]
    public void FrameReady_OnHandle_IsRelayedToSubscribers()
    {
        _player.Start();

        VideoFrame? received = null;
        _player.FrameReady += (_, frame) => received = frame;

        var dataLength = 4 * 4 * 4;
        using var frame = new VideoFrame(ArrayPool<byte>.Shared.Rent(dataLength), 4, 4, dataLength);
        _handle.RaiseFrameReady(frame);

        Assert.IsNotNull(received);
        Assert.AreEqual(4, received.Width);
        Assert.AreEqual(4, received.Height);
    }

    [TestMethod]
    public void StatusChanged_RaisedOnStart()
    {
        var statuses = CaptureStatuses();

        _player.Start();

        Assert.Contains("Connecting...", statuses);
    }

    [TestMethod]
    public void StatusChanged_RelayedFromHandleErrorCallback()
    {
        var statuses = CaptureStatuses();

        _player.Start();
        _factory.CapturedOnError?.Invoke("TLS error");

        Assert.AreEqual("TLS error", statuses[^1]);
    }

    [TestMethod]
    public void Playing_RaisesConnectedStatusImmediately()
    {
        var statuses = CaptureStatuses();

        _player.Start();
        _handle.RaisePlaying();

        Assert.AreEqual("Connected", statuses[^1]);
    }

    [TestMethod]
    public void ScheduleReconnect_WhileStopped_DoesNothing()
    {
        _player.Start();
        _player.Stop();

        _player.ScheduleReconnect("test reason");

        Assert.AreEqual(1, _handle.PlayCallCount);
    }

    [TestMethod]
    public void ScheduleReconnect_RaisesStatusWithReason()
    {
        _player.Start();
        var statuses = CaptureStatuses();

        _player.ScheduleReconnect("Stream ended");

        Assert.AreEqual("Stream ended", statuses[^1]);
    }

    [TestMethod]
    public void ScheduleReconnect_CalledTwice_OnlyFirstTakesEffect()
    {
        _player.Start();
        var statuses = CaptureStatuses();

        _player.ScheduleReconnect("error 1");
        _player.ScheduleReconnect("error 2");

        Assert.HasCount(1, statuses);
        Assert.AreEqual("error 1", statuses[0]);
    }

    [TestMethod]
    public void EncounteredError_OnHandle_TriggersReconnect()
    {
        _player.Start();
        var statuses = CaptureStatuses();

        _handle.RaiseEncounteredError();

        Assert.AreEqual("Playback error", statuses[^1]);
    }

    [TestMethod]
    public void EndReached_OnHandle_TriggersReconnect()
    {
        _player.Start();
        var statuses = CaptureStatuses();

        _handle.RaiseEndReached();

        Assert.AreEqual("Stream ended", statuses[^1]);
    }

    [TestMethod]
    public void TearDownPlayer_DisposesHandle()
    {
        _player.Start();

        _player.TearDownPlayer();

        Assert.IsTrue(_handle.IsDisposed);
    }

    [TestMethod]
    public void TearDownPlayer_WhenNoHandleExists_DoesNotThrow()
    {
        _player.TearDownPlayer();
    }

    [TestMethod]
    public void StartInternal_WhenStopped_DoesNotCreateHandle()
    {
        var factory = new Mock<IVlcPlayerFactory>();
        var player = new RtspVideoPlayer("rtsps://host", factory.Object);
        player.Stop();

        player.StartInternal();

        factory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<Action<string>>()), Times.Never());
    }
}
