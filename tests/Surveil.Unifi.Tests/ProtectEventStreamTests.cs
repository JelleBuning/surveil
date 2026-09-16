using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Events;
using Surveil.Infrastructure.Settings;
using Surveil.Unifi.WebSocket;

namespace Surveil.Unifi.Tests;

[TestClass]
public sealed class ProtectEventStreamTests
{
    private static ProtectEventStream CreateStream(
        IWebSocketFactory webSocketFactory,
        string baseUrl = "https://host",
        string apiKey = "key",
        ISettingsChangeNotifier? notifier = null) =>
        new(TestFixtures.ProtectOptions(baseUrl, apiKey), webSocketFactory, TestFixtures.AllEventsEnabled(), notifier);

    [TestMethod]
    public void BuildWebSocketUri_HttpsBaseUrl_UsesWss()
    {
        var stream = CreateStream(new Mock<IWebSocketFactory>().Object, "https://192.168.0.1/proxy/protect/api");

        var uri = stream.BuildWebSocketUri();

        Assert.AreEqual("wss", uri.Scheme);
        Assert.AreEqual("192.168.0.1", uri.Host);
        Assert.IsTrue(uri.AbsolutePath.EndsWith("/v1/subscribe/events"));
    }

    [TestMethod]
    public void BuildWebSocketUri_HttpBaseUrl_UsesWs()
    {
        var stream = CreateStream(new Mock<IWebSocketFactory>().Object, "http://192.168.0.1/api");

        var uri = stream.BuildWebSocketUri();

        Assert.AreEqual("ws", uri.Scheme);
    }

    [TestMethod]
    public void BuildWebSocketUri_TrailingSlash_HandledCorrectly()
    {
        var stream = CreateStream(new Mock<IWebSocketFactory>().Object, "https://host/api/");

        var uri = stream.BuildWebSocketUri();

        Assert.AreEqual("wss", uri.Scheme);
        Assert.IsTrue(uri.AbsolutePath.EndsWith("v1/subscribe/events"));
    }

    [TestMethod]
    public async Task SubscribeAsync_AlreadyCancelled_YieldsNoEvents()
    {
        var wsFactoryMock = new Mock<IWebSocketFactory>();
        var stream = CreateStream(wsFactoryMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
            events.Add(e);

        Assert.IsEmpty(events);
        wsFactoryMock.Verify(f => f.Create(It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task SubscribeAsync_ConnectFails_RetriesAfterTheBackoffDelay()
    {
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(w => w.State).Returns(WebSocketState.Closed);
        wsMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
              .Returns(Task.FromException(new WebSocketException("refused")));

        var wsFactoryMock = new Mock<IWebSocketFactory>();
        wsFactoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(wsMock.Object);

        var stream = CreateStream(wsFactoryMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
            events.Add(e);

        Assert.IsEmpty(events);
        wsFactoryMock.Verify(f => f.Create(It.IsAny<string>()), Times.AtLeast(2));
    }

    [TestMethod]
    public async Task SubscribeAsync_ConnectCancelledImmediately_YieldsNoEvents()
    {
        using var cts = new CancellationTokenSource();

        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
              .Returns((Uri _, CancellationToken ct) =>
              {
                  cts.Cancel();
                  return Task.FromCanceled(ct);
              });

        var wsFactoryMock = new Mock<IWebSocketFactory>();
        wsFactoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(wsMock.Object);

        var stream = CreateStream(wsFactoryMock.Object);

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
            events.Add(e);

        Assert.IsEmpty(events);
    }

    [TestMethod]
    public async Task SubscribeAsync_ConnectsAndReceivesEvent_YieldsEvent()
    {
        const string json = """{"type":"add","item":{"id":"ev1","type":"ring","start":1000,"device":"dev1"}}""";
        var stream = CreateStream(TestFixtures.WebSocketDelivering(json));
        using var cts = new CancellationTokenSource(5000);

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
        {
            events.Add(e);
            break;
        }

        Assert.ContainsSingle(events);
        Assert.AreEqual("ev1", events[0].Id);
        Assert.AreEqual("dev1", events[0].DeviceId);
        Assert.AreEqual("Doorbell ring", events[0].Description);
    }

    [TestMethod]
    public async Task SubscribeAsync_ReceivesCloseMessage_ReconnectsWithBackoff()
    {
        var receiveCount = 0;
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        wsMock.SetupGet(w => w.State).Returns(WebSocketState.Open);
        wsMock.Setup(w => w.ReceiveAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
              .Returns(() =>
              {
                  receiveCount++;
                  return new ValueTask<ValueWebSocketReceiveResult>(
                      new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
              });

        var wsFactoryMock = new Mock<IWebSocketFactory>();
        wsFactoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(wsMock.Object);

        var stream = CreateStream(wsFactoryMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
            events.Add(e);

        Assert.IsEmpty(events);
        Assert.IsTrue(receiveCount > 0, "At least one receive call was made");
        wsFactoryMock.Verify(f => f.Create(It.IsAny<string>()), Times.AtLeast(2));
    }

    [TestMethod]
    public void SettingsChanged_UpdatesBuildWebSocketUri()
    {
        var notifier = new SettingsChangeNotifier();
        var stream = CreateStream(new Mock<IWebSocketFactory>().Object, "https://host1", "key1", notifier);

        Assert.AreEqual("host1", stream.BuildWebSocketUri().Host);

        notifier.NotifyChanged(new AppSettings
        {
            SelectedProvider = VideoProviderType.UnifiProtect,
            UnifiProtect = new UnifiProtectProviderSettings { BaseUrl = "https://host2", ApiKey = "key2" }
        });

        Assert.AreEqual("host2", stream.BuildWebSocketUri().Host);
    }

    [TestMethod]
    public async Task SettingsChanged_DuringConnect_AbortsAndReconnectsWithNewApiKey()
    {
        var notifier = new SettingsChangeNotifier();
        var capturedApiKeys = new List<string>();
        var firstConnectStarted = new TaskCompletionSource();

        var wsFactoryMock = new Mock<IWebSocketFactory>();
        wsFactoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns((string apiKey) =>
        {
            capturedApiKeys.Add(apiKey);
            return capturedApiKeys.Count == 1 ? ConnectHangsUntilAborted() : ConnectsThenClosesImmediately();
        });

        var stream = CreateStream(wsFactoryMock.Object, apiKey: "old-key", notifier: notifier);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var readTask = Task.Run(async () =>
        {
            await foreach (var _ in stream.SubscribeAsync(cts.Token)) { }
        });

        await firstConnectStarted.Task;

        notifier.NotifyChanged(new AppSettings
        {
            SelectedProvider = VideoProviderType.UnifiProtect,
            UnifiProtect = new UnifiProtectProviderSettings { BaseUrl = "https://host", ApiKey = "new-key" }
        });

        await readTask;

        CollectionAssert.Contains(capturedApiKeys, "new-key");

        IWebSocketConnection ConnectHangsUntilAborted()
        {
            var wsMock = new Mock<IWebSocketConnection>();
            wsMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                  .Returns((Uri _, CancellationToken ct) =>
                  {
                      firstConnectStarted.TrySetResult();
                      return Task.Delay(Timeout.Infinite, ct);
                  });
            return wsMock.Object;
        }

        static IWebSocketConnection ConnectsThenClosesImmediately()
        {
            var wsMock = new Mock<IWebSocketConnection>();
            wsMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            wsMock.SetupGet(w => w.State).Returns(WebSocketState.Open);
            wsMock.Setup(w => w.ReceiveAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                  .Returns(new ValueTask<ValueWebSocketReceiveResult>(
                      new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true)));
            return wsMock.Object;
        }
    }
}
