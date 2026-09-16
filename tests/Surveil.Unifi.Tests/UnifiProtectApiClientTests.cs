using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Surveil.Unifi.Tests;

internal sealed class StubHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
            RequestMessage = new HttpRequestMessage()
        });
}

[TestClass]
public sealed class UnifiProtectApiClientTests
{
    private static UnifiProtectApiClient CreateClient(
        string body, HttpStatusCode status = HttpStatusCode.OK, string? reachableHost = null)
    {
        var http = new HttpClient(new StubHttpHandler(status, body)) { BaseAddress = new Uri("https://host/api/") };
        return new UnifiProtectApiClient(http, reachableHost);
    }

    private static string StreamsJson(
        string? high = null, string? medium = null, string? low = null, string? package = null) =>
        $$"""{"high":{{Quote(high)}},"medium":{{Quote(medium)}},"low":{{Quote(low)}},"package":{{Quote(package)}}}""";

    private static string Quote(string? url) => url is null ? "null" : $"\"{url}\"";

    [TestMethod]
    public async Task GetCamerasAsync_ValidJson_ReturnsMappedCameras()
    {
        const string json = """
            [
              {"id":"cam1","name":"Front Door","state":"CONNECTED"},
              {"id":"cam2","name":"Backyard","state":"DISCONNECTED"}
            ]
            """;
        var client = CreateClient(json);

        var cameras = await client.GetCamerasAsync();

        Assert.HasCount(2, cameras);
        Assert.AreEqual("cam1", cameras[0].Id);
        Assert.AreEqual("Front Door", cameras[0].Name);
        Assert.IsTrue(cameras[0].IsConnected);
        Assert.AreEqual("cam2", cameras[1].Id);
        Assert.IsFalse(cameras[1].IsConnected);
    }

    [TestMethod]
    public async Task GetCamerasAsync_EmptyArray_ReturnsEmptyList()
    {
        var client = CreateClient("[]");

        var cameras = await client.GetCamerasAsync();

        Assert.IsEmpty(cameras);
    }

    [TestMethod]
    public async Task GetCamerasAsync_ServerError_ThrowsHttpRequestException()
    {
        var client = CreateClient("Unauthorized", HttpStatusCode.Unauthorized);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetCamerasAsync());

        Assert.AreEqual(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [TestMethod]
    public async Task GetCamerasAsync_LongErrorBody_TruncatesTo300Chars()
    {
        var client = CreateClient(new string('x', 400), HttpStatusCode.InternalServerError);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetCamerasAsync());

        Assert.Contains("…", ex.Message);
    }

    [TestMethod]
    public async Task GetCamerasAsync_InvalidJson_ThrowsInvalidOperationException()
    {
        var client = CreateClient("not-json");

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetCamerasAsync());
    }

    [TestMethod]
    public async Task GetRtspsStreamsAsync_HighQualityPresent_ReturnsHighStream()
    {
        var client = CreateClient(StreamsJson(high: "rtsps://host/high"));

        var streams = await client.GetRtspsStreamsAsync("cam1");

        Assert.ContainsSingle(streams);
        Assert.AreEqual("rtsp://host/high", streams[0].Url);
        Assert.AreEqual("high", streams[0].StreamName);
    }

    [TestMethod]
    public async Task GetRtspsStreamsAsync_NoHighOnlyMedium_ReturnsMediumStream()
    {
        var client = CreateClient(StreamsJson(medium: "rtsps://host/medium"));

        var streams = await client.GetRtspsStreamsAsync("cam1");

        Assert.ContainsSingle(streams);
        Assert.AreEqual("medium", streams[0].StreamName);
    }

    [TestMethod]
    public async Task GetRtspsStreamsAsync_OnlyLow_ReturnsLowStream()
    {
        var client = CreateClient(StreamsJson(low: "rtsps://host/low"));

        var streams = await client.GetRtspsStreamsAsync("cam1");

        Assert.ContainsSingle(streams);
        Assert.AreEqual("low", streams[0].StreamName);
    }

    [TestMethod]
    public async Task GetRtspsStreamsAsync_OnlyPackage_ReturnsPackageStream()
    {
        var client = CreateClient(StreamsJson(package: "rtsps://host/pkg"));

        var streams = await client.GetRtspsStreamsAsync("cam1");

        Assert.ContainsSingle(streams);
        Assert.AreEqual("package", streams[0].StreamName);
    }

    [TestMethod]
    public async Task GetRtspsStreamsAsync_AllNull_ReturnsEmptyList()
    {
        var client = CreateClient(StreamsJson());

        var streams = await client.GetRtspsStreamsAsync("cam1");

        Assert.IsEmpty(streams);
    }

    [TestMethod]
    public async Task GetRtspsStreamsAsync_PortAndSrtpQuery_NormalizedForLibVlc()
    {
        var client = CreateClient(StreamsJson(high: "rtsps://host:7441/stream?enableSrtp"));

        var streams = await client.GetRtspsStreamsAsync("cam1");

        Assert.AreEqual("rtsp://host:7447/stream", streams[0].Url);
    }

    [TestMethod]
    public async Task GetRtspsStreamsAsync_HostDiffersFromReachableHost_RewritesHost()
    {
        var client = CreateClient(StreamsJson(high: "rtsps://console-lan-ip/stream"), reachableHost: "host");

        var streams = await client.GetRtspsStreamsAsync("cam1");

        Assert.AreEqual("rtsp://host/stream", streams[0].Url);
    }

    [TestMethod]
    public async Task GetRtspsStreamsAsync_ServerError_ThrowsHttpRequestException()
    {
        var client = CreateClient("Not found", HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetRtspsStreamsAsync("cam1"));
    }

    [TestMethod]
    public async Task CreateRtspsStreamAsync_SuccessWithHighUrl_ReturnsBestStream()
    {
        var client = CreateClient(StreamsJson(high: "rtsps://host/high"));

        var stream = await client.CreateRtspsStreamAsync("cam1");

        Assert.AreEqual("rtsp://host/high", stream.Url);
        Assert.AreEqual("high", stream.StreamName);
    }

    [TestMethod]
    public async Task CreateRtspsStreamAsync_AllUrlsNull_ThrowsInvalidOperationException()
    {
        var client = CreateClient(StreamsJson());

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateRtspsStreamAsync("cam1"));
    }

    [TestMethod]
    public async Task CreateRtspsStreamAsync_ServerError_ThrowsHttpRequestException()
    {
        var client = CreateClient("Bad request", HttpStatusCode.BadRequest);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.CreateRtspsStreamAsync("cam1"));
    }

    [TestMethod]
    public async Task CreateRtspsStreamAsync_WithCancellationToken_PassesToken()
    {
        var client = CreateClient(StreamsJson(high: "rtsps://host/high"));
        using var cts = new CancellationTokenSource();

        var stream = await client.CreateRtspsStreamAsync("cam1", cts.Token);

        Assert.IsNotNull(stream);
    }
}
