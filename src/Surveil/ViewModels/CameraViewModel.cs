using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Options;
using Surveil.Application.Ports;
using Surveil.Domain.Cameras;
using Surveil.Services;

namespace Surveil.ViewModels;

public sealed class CameraViewModel : ObservableObject, IDisposable
{
    private readonly ICameraProvider _apiClient;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SnapshotService _snapshotService;
    private readonly CancellationTokenSource _cts = new();

    private WriteableBitmap? _videoBitmap;
    private bool _updatePending;
    private RtspVideoPlayer? _player;

    public WriteableBitmap? VideoSource
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string StatusMessage
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public CameraViewModel(
        Camera camera,
        ICameraProvider apiClient,
        IOptions<UnifiProtectOptions> options,
        DispatcherQueue dispatcherQueue)
    {
        _apiClient = apiClient;
        _dispatcherQueue = dispatcherQueue;

        var snapshotPath = options.Value.SnapshotPath
            ?? Path.Combine(AppContext.BaseDirectory, "snapshots", "snapshot.jpg");
        _snapshotService = new SnapshotService(snapshotPath);

        _ = StartStreamAsync(camera, _cts.Token);
    }

    private async Task StartStreamAsync(Camera camera, CancellationToken ct)
    {
        try
        {
            UpdateStatus($"Connecting to {camera.Name}...");

            var streams = await _apiClient.GetRtspsStreamsAsync(camera.Id, ct);
            var stream  = streams.FirstOrDefault()
                          ?? await _apiClient.CreateRtspsStreamAsync(camera.Id, ct);

            _player = new RtspVideoPlayer(stream.Url);
            _player.FrameReady    += OnFrameReady;
            _player.StatusChanged += OnStatusChanged;
            await Task.Run(_player.Start, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CameraViewModel] Stream start failed: {ex.Message}");
            UpdateStatus($"Error: {ex.Message}");
        }
    }

    private void OnStatusChanged(object? sender, string message) => UpdateStatus(message);

    private void UpdateStatus(string message) =>
        _dispatcherQueue.TryEnqueue(() => StatusMessage = message);

    private void OnFrameReady(object? sender, VideoFrame frame)
    {
        if (_updatePending)
        {
            frame.Dispose();
            return;
        }

        _updatePending = true;
        var queued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureBitmap(frame.Width, frame.Height);
                using var stream = _videoBitmap!.PixelBuffer.AsStream();
                stream.Write(frame.Pixels, 0, frame.DataLength);
                _videoBitmap.Invalidate();
                _snapshotService.CaptureFrame(frame.Width, frame.Height, frame.Pixels);
            }
            finally
            {
                frame.Dispose();
                _updatePending = false;
            }
        });

        if (!queued)
        {
            frame.Dispose();
            _updatePending = false;
        }
    }

    private void EnsureBitmap(int width, int height)
    {
        if (_videoBitmap is null || _videoBitmap.PixelWidth != width || _videoBitmap.PixelHeight != height)
        {
            _videoBitmap = new WriteableBitmap(width, height);
            VideoSource = _videoBitmap;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();

        if (_player is not null)
        {
            _player.FrameReady    -= OnFrameReady;
            _player.StatusChanged -= OnStatusChanged;
            _player.Dispose();
        }

        _snapshotService.Dispose();
    }
}
