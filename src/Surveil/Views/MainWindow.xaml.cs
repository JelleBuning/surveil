using H.NotifyIcon;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using Surveil.Application.Options;
using Surveil.Application.Ports;
using Surveil.Domain.Cameras;
using Surveil.Services.Interfaces;
using Surveil.ViewModels;
using Windows.Graphics;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Surveil.Views;

public sealed partial class MainWindow
{
    private const int WindowWidth = 1350;
    private const int WindowHeight = 800;

    public MainViewModel ViewModel { get; }

    public MainWindow(
        ICameraProvider apiClient,
        IProtectEventStream eventStream,
        IDesktopNotifier notifier,
        ISettingsChangeNotifier settingsNotifier,
        IOptions<EventNotificationSettings> eventSettings)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ResizeAndCenter();

        ViewModel = new MainViewModel(
            this,
            apiClient,
            eventStream,
            notifier,
            settingsNotifier,
            eventSettings.Value,
            DispatcherQueue.GetForCurrentThread());
        RootGrid.DataContext = ViewModel;

        ViewModel.Cameras.CollectionChanged += OnCamerasChanged;

        Closed += OnWindowClosed;

        TaskBarIcon.ForceCreate();
    }

    private void OnCamerasChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            CamerasGroup.MenuItems.Clear();
            return;
        }

        if (e.Action != NotifyCollectionChangedAction.Add) return;

        foreach (Camera camera in e.NewItems!)
        {
            var item = new NavigationViewItem
            {
                Content = camera.Name,
                Tag     = camera
            };
            ToolTipService.SetToolTip(item, camera.Name);
            CamerasGroup.MenuItems.Add(item);
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage), null, new SuppressNavigationTransitionInfo());
            return;
        }

        if (args.SelectedItem is not NavigationViewItem { Tag: Camera camera }) return;

        ViewModel.SelectedCamera = camera;
        ContentFrame.Navigate(typeof(CameraView), camera, new SuppressNavigationTransitionInfo());
    }

    public void BringToFront()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        ShowWindow(hwnd, 9);
        SetForegroundWindow(hwnd);
    }

    public void ShowFromBackground() => DispatcherQueue.TryEnqueue(BringToFront);

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        args.Handled = true;
        this.Hide();
    }

    private void ResizeAndCenter()
    {
        var appWindow = AppWindow.GetFromWindowId(AppWindow.Id);
        var display  = DisplayArea.Primary;
        var x = (display.OuterBounds.Width  - WindowWidth)  / 2;
        var y = (display.OuterBounds.Height - WindowHeight) / 2;
        appWindow.MoveAndResize(new RectInt32(x, y, WindowWidth, WindowHeight));
        appWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);
}
