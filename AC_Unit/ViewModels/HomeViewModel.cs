
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Iot_Recources.Data;
using Iot_Recources.Services;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

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

        _deviceManager.OnDeviceStateChanged += UpdateDeviceState;
        StartListeningForMessages();
    }

    [ObservableProperty]
    public string _pageTitle = "Home page";

    [ObservableProperty]
    private string _deviceState = "Off";

    [ObservableProperty]
    private string _toggleButtonText = "Turn On";

    private void StartListeningForMessages()
    {
        try
        {
            _cts = new CancellationTokenSource();
            Task.Run(async () => await _deviceManager.ReceiveCloudToDeviceMessagesAsync(CancellationToken.None));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not start listening to messages. {ex.Message}");
        }
    }

    private void UpdateToggleButtonText()
    {
        ToggleButtonText = DeviceState == "On" ? "Turn Off" : "Turn On";
    }

    public void UpdateDeviceState(string newState)
    {
        DeviceState = newState;
        UpdateToggleButtonText();

        bool isDeviceOn = DeviceState == "Off";
        _deviceTwinManager.UpdateDeviceTwinAsync(isDeviceOn).ConfigureAwait(false);

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
        Environment.Exit(0);
    }
}
