using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Options;
using Surveil.Application.Ports;
using Surveil.Domain.Cameras;

namespace Surveil.Infrastructure.Http;

public sealed class UnifiProtectApiClient : ICameraProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _reachableHost;

    public UnifiProtectApiClient(IOptions<UnifiProtectOptions> options)
    {
        var opts = options.Value;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri($"{opts.BaseUrl.TrimEnd('/')}/{UnifiProtectOptions.ApiPath}/")
        };

        _http.DefaultRequestHeaders.Add("Accept", "application/json");
        _http.DefaultRequestHeaders.Add("X-API-KEY", opts.ApiKey);

        _reachableHost = new Uri(opts.BaseUrl).Host;
    }

    internal UnifiProtectApiClient(HttpClient httpClient, string? reachableHost = null)
    {
        _http = httpClient;
        _reachableHost = reachableHost ?? httpClient.BaseAddress?.Host ?? string.Empty;
    }

    public async Task<IReadOnlyList<Camera>> GetCamerasAsync(CancellationToken ct = default)
    {
        var dtos = await GetJsonAsync<List<CameraDto>>("v1/cameras", ct) ?? [];
        return dtos.ConvertAll(d => new Camera(d.Id, d.Name, d.State == "CONNECTED"));
    }

    public async Task<IReadOnlyList<RtspsStream>> GetRtspsStreamsAsync(string cameraId, CancellationToken ct = default)
    {
        var dto = await GetJsonAsync<RtspsStreamDto>($"v1/cameras/{cameraId}/rtsps-stream", ct);
        var best = dto?.BestStream();
        return best.HasValue ? [new RtspsStream(NormalizeForLibVlc(best.Value.Url), best.Value.Quality)] : [];
    }

    public async Task<RtspsStream> CreateRtspsStreamAsync(string cameraId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"v1/cameras/{cameraId}/rtsps-stream", content: null, ct);
        await EnsureSuccessAsync(response, ct);
        var dto = await DeserializeAsync<RtspsStreamDto>(response, ct);
        var best = dto?.BestStream()
                   ?? throw new InvalidOperationException("No usable RTSPS URL in CreateRtspsStream response.");
        return new RtspsStream(NormalizeForLibVlc(best.Url), best.Quality);
    }

    /// <summary>
    /// LibVLC 3.x cannot handle RTSPS (TLS) or SRTP, so this rewrites to plain RTSP on
    /// the unencrypted media port (7447). It also corrects the host to the one used to
    /// reach the API, since the console can report an RTSP host (e.g. a stale/secondary
    /// interface) that isn't reachable from wherever this client is running (notably: a VPN).
    /// </summary>
    private string NormalizeForLibVlc(string url)
    {
        var rewritten = url
            .Replace("rtsps://", "rtsp://")
            .Replace(":7441/", ":7447/")
            .Replace("?enableSrtp", "")
            .TrimEnd('?');

        var streamUri = new Uri(rewritten);
        if (string.IsNullOrEmpty(_reachableHost) ||
            string.Equals(streamUri.Host, _reachableHost, StringComparison.OrdinalIgnoreCase))
            return rewritten;

        return new UriBuilder(streamUri) { Host = _reachableHost }.Uri.ToString();
    }

    /// <summary>
    /// GET helper: checks HTTP status first, then deserializes.
    /// On deserialization failure, includes the raw body in the exception.
    /// </summary>
    private async Task<T?> GetJsonAsync<T>(string requestUri, CancellationToken ct)
    {
        var response = await _http.GetAsync(requestUri, ct);
        await EnsureSuccessAsync(response, ct);
        return await DeserializeAsync<T>(response, ct);
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        Debug.WriteLine($"[UnifiProtectApiClient] {response.RequestMessage?.RequestUri} → {body[..Math.Min(body.Length, 500)]}");
        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize response from {response.RequestMessage?.RequestUri} as {typeof(T).Name}. " +
                $"Body: {body[..Math.Min(body.Length, 500)]}", ex);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = string.Empty;
        try { body = await response.Content.ReadAsStringAsync(ct); }
        catch { /* best-effort */ }

        if (body.Length > 300)
            body = body[..300] + "…";

        Debug.WriteLine($"[UnifiProtectApiClient] HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        throw new HttpRequestException(
            $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} — {body}",
            inner: null,
            statusCode: response.StatusCode);
    }

    private sealed record CameraDto(string Id, string Name, string State);

    /// <summary>Quality-keyed RTSPS stream object returned by the API.</summary>
    private sealed record RtspsStreamDto(
        string? High,
        string? Medium,
        string? Low,
        string? Package)
    {
        /// <summary>Returns the best available stream URL and its quality label, or null if none.</summary>
        public (string Url, string Quality)? BestStream() =>
            High   is not null ? (High,   "high")   :
            Medium is not null ? (Medium, "medium") :
            Low    is not null ? (Low,    "low")    :
            Package is not null ? (Package, "package") :
            null;
    }
}
