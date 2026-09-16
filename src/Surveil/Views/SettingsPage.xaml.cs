using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Surveil.ViewModels;

namespace Surveil.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var viewModel = Ioc.Default.GetRequiredService<SettingsViewModel>();
        DataContext = viewModel;

        await viewModel.InitializeAsync();
    }
}
