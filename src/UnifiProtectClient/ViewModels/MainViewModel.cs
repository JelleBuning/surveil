using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnifiProtectClient.Application.Options;
using UnifiProtectClient.Application.Ports;
using UnifiProtectClient.Application.Settings;
using UnifiProtectClient.Domain.Cameras;
using UnifiProtectClient.Domain.Events;
using UnifiProtectClient.Services.Interfaces;
using UnifiProtectClient.Views;
using System.Linq;

namespace UnifiProtectClient.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly MainWindow _mainWindow;
    private readonly ICameraProvider _apiClient;
    private readonly IProtectEventStream _eventStream;
    private readonly IDesktopNotifier _notifier;
    private readonly ISettingsChangeNotifier _settingsNotifier;
    private readonly EventNotificationSettings _eventSettings;
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
        IProtectEventStream eventStream,
        IDesktopNotifier notifier,
        ISettingsChangeNotifier settingsNotifier,
        EventNotificationSettings eventSettings,
        DispatcherQueue dispatcherQueue)
    {
        _mainWindow = mainWindow;
        _apiClient = apiClient;
        _eventStream = eventStream;
        _notifier = notifier;
        _settingsNotifier = settingsNotifier;
        _eventSettings = eventSettings;
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
            {
                if (_eventSettings.IsEnabled(@event) && IsNotifiableEvent(@event))
                    _notifier.Notify(@event, _selectedCamera?.Name ?? "Unknown Camera");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainViewModel] Event stream error: {ex.Message}");
        }
    }

    // Notify on Add events for all types, and also on Update events for ring events
    // that have no End timestamp yet (ring is starting, not ending). The integration
    // API at /proxy/protect/integration emits ring events as Update, not Add.
    private static bool IsNotifiableEvent(ProtectEvent @event) =>
        @event.UpdateType == ProtectEventUpdateType.Add ||
        @event is RingEvent { End: null };

    [RelayCommand]
    public void LeftClick() => _mainWindow.BringToFront();

    public void Dispose()
    {
        _settingsNotifier.SettingsChanged -= OnSettingsChanged;
        _cts.Cancel();
        _cts.Dispose();
    }
}
