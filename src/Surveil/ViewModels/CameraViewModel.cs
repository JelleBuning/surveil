using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Cameras;
using Surveil.Services;

namespace Surveil.ViewModels;

public sealed class CameraViewModel : ObservableObject, IDisposable
{
    private readonly ICameraProvider _apiClient;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SnapshotService _snapshotService;
    private readonly CancellationTokenSource _cts = new();

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
        SnapshotOptions snapshot,
        DispatcherQueue dispatcherQueue)
    {
        _apiClient = apiClient;
        _dispatcherQueue = dispatcherQueue;

        _snapshotService = new SnapshotService(snapshot.Path);

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
                var bitmap = EnsureBitmap(frame.Width, frame.Height);
                using var stream = bitmap.PixelBuffer.AsStream();
                stream.Write(frame.Pixels, 0, frame.DataLength);
                bitmap.Invalidate();
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

    private WriteableBitmap EnsureBitmap(int width, int height)
    {
        var bitmap = VideoSource;

        if (bitmap is null || bitmap.PixelWidth != width || bitmap.PixelHeight != height)
        {
            bitmap = new WriteableBitmap(width, height);
            VideoSource = bitmap;
        }

        return bitmap;
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
