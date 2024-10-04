using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AC_Unit.ViewModels;

public partial class SettingsViewModel : ObservableObject
{

    private readonly IServiceProvider _serviceProvider;

    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [ObservableProperty]
    private string _pageTitle = "Settings page";

    [RelayCommand]
    private void GoToHome()
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.CurrentViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
    }

    [RelayCommand]
    private void CloseApp()
    {
        Environment.Exit(0);
    }
}


