using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnifiProtectClient.Application.Options;
using UnifiProtectClient.Application.Ports;
using UnifiProtectClient.Domain.Cameras;
using UnifiProtectClient.ViewModels;

namespace UnifiProtectClient.Views;

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
        var options   = Ioc.Default.GetRequiredService<IOptions<UnifiProtectOptions>>();

        _viewModel = new CameraViewModel(camera, apiClient, options, DispatcherQueue);
        DataContext = _viewModel;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _viewModel?.Dispose();
        _viewModel = null;
    }
}
