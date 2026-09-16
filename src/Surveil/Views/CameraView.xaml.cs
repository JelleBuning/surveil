using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Cameras;
using Surveil.ViewModels;

namespace Surveil.Views;

public sealed partial class CameraView : Page
{
    private CameraViewModel? _viewModel;

    public CameraView()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Camera camera) return;

        var apiClient = Ioc.Default.GetRequiredService<ICameraProvider>();
        var snapshot = Ioc.Default.GetRequiredService<SnapshotOptions>();

        _viewModel = new CameraViewModel(camera, apiClient, snapshot, DispatcherQueue);
        DataContext = _viewModel;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        _viewModel?.Dispose();
        _viewModel = null;
    }
}
