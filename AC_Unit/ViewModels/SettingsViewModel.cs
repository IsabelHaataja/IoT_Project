using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Iot_Recources.Data;
using Iot_Recources.Factories;
using Iot_Recources.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace AC_Unit.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IDatabaseContext _context;
    private readonly IServiceProvider _serviceProvider;

    public SettingsViewModel(IServiceProvider serviceProvider, IDatabaseContext context)
    {
        _serviceProvider = serviceProvider;
        _context = context;
        GetDeviceSettingsAsync().ConfigureAwait(false);
    }

    [ObservableProperty]
    private string pageTitle = "Settings page";

    [ObservableProperty]
    private string deviceId;

    [ObservableProperty]
    private bool isConfigured = false;

    [ObservableProperty]
    private DeviceSettings? settings;

    [RelayCommand]
    public async Task ConfigureSettings()
    {
        await _context.SaveSettingsAsync(DeviceSettingsFactory.Create());
        await GetDeviceSettingsAsync();
    }

    [RelayCommand]
    public async Task ResetSettings()
    {
        await _context.ResetSettingsAsync();
        await GetDeviceSettingsAsync();
    }


    public async Task GetDeviceSettingsAsync()
    {
        var response = await _context.GetSettingsAsync();
        Settings = response.Result;
        IsConfigured = Settings != null;
        DeviceId = await _context.GetDeviceIdFromConnectionStringAsync();
    }

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


