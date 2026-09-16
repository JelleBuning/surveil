using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Cameras;
using Surveil.Services.Interfaces;
using Surveil.Views;

namespace Surveil.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly MainWindow _mainWindow;
    private readonly ICameraProvider _apiClient;
    private readonly ICameraEventStream _eventStream;
    private readonly IDesktopNotifier _notifier;
    private readonly ISettingsChangeNotifier _settingsNotifier;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly CancellationTokenSource _cts = new();

    private Camera? _selectedCamera;

    public ObservableCollection<Camera> Cameras { get; } = [];

    public Camera? SelectedCamera
    {
        get => _selectedCamera;
        set => SetProperty(ref _selectedCamera, value);
    }

    public MainViewModel(
        MainWindow mainWindow,
        ICameraProvider apiClient,
        ICameraEventStream eventStream,
        IDesktopNotifier notifier,
        ISettingsChangeNotifier settingsNotifier,
        DispatcherQueue dispatcherQueue)
    {
        _mainWindow = mainWindow;
        _apiClient = apiClient;
        _eventStream = eventStream;
        _notifier = notifier;
        _settingsNotifier = settingsNotifier;
        _dispatcherQueue = dispatcherQueue;

        _settingsNotifier.SettingsChanged += OnSettingsChanged;

        _ = InitializeCamerasAsync(_cts.Token);
        _ = SubscribeToEventsAsync(_cts.Token);
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        _dispatcherQueue.TryEnqueue(() => Cameras.Clear());
        _ = InitializeCamerasAsync(_cts.Token);
    }

    private async Task InitializeCamerasAsync(CancellationToken ct)
    {
        try
        {
            var cameras = await _apiClient.GetCamerasAsync(ct);
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var camera in cameras)
                    Cameras.Add(camera);

                SelectedCamera = cameras.Count > 0 ? cameras[0] : null;
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainViewModel] Camera discovery failed: {ex.Message}");
        }
    }

    private async Task SubscribeToEventsAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var @event in _eventStream.SubscribeAsync(ct))
                _notifier.Notify(@event, _selectedCamera?.Name ?? "Unknown Camera");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainViewModel] Event stream error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void LeftClick() => _mainWindow.BringToFront();

    [RelayCommand]
    public void Exit() => _mainWindow.ExitApplication();

    public void Dispose()
    {
        _settingsNotifier.SettingsChanged -= OnSettingsChanged;
        _cts.Cancel();
        _cts.Dispose();
    }
}
