using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Domain.Events;

namespace Surveil.Unifi.Tests;

[TestClass]
public sealed class ProtectEventStreamFilteringTests
{
    private static async Task<List<CameraEvent>> CollectAsync(string json, EventNotificationSettings settings)
    {
        var stream = new ProtectEventStream(
            TestFixtures.ProtectOptions(), TestFixtures.WebSocketDelivering(json), settings);
        using var cts = new CancellationTokenSource(500);

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
        {
            events.Add(e);
            break;
        }

        return events;
    }

    [TestMethod]
    public async Task DisabledEventType_IsNotEmitted()
    {
        const string json = """{"type":"add","item":{"id":"ev1","type":"motion","start":1000,"device":"dev1"}}""";

        var events = await CollectAsync(json, new EventNotificationSettings { Motion = false });

        Assert.IsEmpty(events);
    }

    [TestMethod]
    public async Task EnabledEventType_IsEmitted()
    {
        const string json = """{"type":"add","item":{"id":"ev1","type":"motion","start":1000,"device":"dev1"}}""";

        var events = await CollectAsync(json, new EventNotificationSettings { Motion = true });

        Assert.ContainsSingle(events);
        Assert.AreEqual("Motion detected", events[0].Description);
    }

    [TestMethod]
    public async Task NonNotifiableUpdate_IsNotEmitted()
    {
        const string json = """{"type":"update","item":{"id":"ev1","type":"motion","start":1000,"device":"dev1"}}""";

        var events = await CollectAsync(json, TestFixtures.AllEventsEnabled());

        Assert.IsEmpty(events);
    }

    [TestMethod]
    public async Task RingUpdateWithoutEnd_IsEmitted()
    {
        const string json = """{"type":"update","item":{"id":"ev1","type":"ring","start":1000,"device":"dev1"}}""";

        var events = await CollectAsync(json, TestFixtures.AllEventsEnabled());

        Assert.ContainsSingle(events);
        Assert.AreEqual("Doorbell ring", events[0].Description);
    }
}
