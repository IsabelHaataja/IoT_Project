
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Iot_Recources.Data;
using Iot_Recources.Services;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;

namespace AC_Unit.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private IDeviceTwinManager _deviceTwinManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDeviceManager _deviceManager;
    private CancellationTokenSource _cts;

    public HomeViewModel(IServiceProvider serviceProvider, IDeviceManager deviceManager, IDeviceTwinManager deviceTwinManager)
    {
        _serviceProvider = serviceProvider;
        _deviceManager = deviceManager;
        _deviceTwinManager = deviceTwinManager;

        StartListeningForMessages().ConfigureAwait(false);
        _deviceManager.OnDeviceStateChanged += UpdateDeviceState;
    }

    [ObservableProperty]
    public string _pageTitle = "Home page";

    [ObservableProperty]
    private string _deviceState = "Off";

    [ObservableProperty]
    private string _toggleButtonText = "Off";

    private async Task StartListeningForMessages()
    {
        try
        {
            Console.WriteLine("Starting to listen for messages...");
            _cts = new CancellationTokenSource();
            await _deviceManager.ReceiveCloudToDeviceMessagesAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not start listening to messages. {ex.Message}");
        }
    }

    private void UpdateToggleButtonText()
    {
        ToggleButtonText = DeviceState == "On" ? "OFF" : "ON";
    }

    public void UpdateDeviceState(string newState)
    {
        try
        {
            Console.WriteLine("Updating device state..");
            DeviceState = newState;
            UpdateToggleButtonText();

            bool isDeviceOn = newState == "On";
            _deviceTwinManager.UpdateDeviceTwinAsync(isDeviceOn).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine("failed to update device state...");
        }
    }

    [RelayCommand]
    private void GoToSettings()
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.CurrentViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
    }
    [RelayCommand]
    private void CloseApp()
    {
        _cts?.Cancel();
        _deviceManager.DisconnectAsync(_cts!.Token);
        Environment.Exit(0);
    }
}
