
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
    private readonly IServiceProvider _serviceProvider;
    private readonly IDeviceManager _deviceManager;
    private CancellationTokenSource _cts;

    public HomeViewModel(IServiceProvider serviceProvider, IDeviceManager deviceManager)
    {
        _serviceProvider = serviceProvider;
        _deviceManager = deviceManager;

        _deviceManager.OnDeviceStateChanged += UpdateDeviceState;
        StartListeningForMessages().ConfigureAwait(false);
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
            // Ensure the device is connected first
            var dbContext = _serviceProvider.GetRequiredService<IDatabaseContext>();
            var deviceConnectionString = await dbContext.GetDeviceConnectionStringAsync();

            if (string.IsNullOrEmpty(deviceConnectionString))
            {
                var registrationResult = await _deviceManager.RegisterDeviceAsync("AC-45ffebf0");
                if (registrationResult.Succeeded)
                {
                    deviceConnectionString = registrationResult.Result;
                }
                else
                {
                    Console.WriteLine($"Failed to register device: {registrationResult.Error}");
                    return;
                }
            }

            var connectionResult = await _deviceManager.ConnectToIotHubAsync(deviceConnectionString);
            if (!connectionResult.Succeeded)
            {
                Console.WriteLine($"Failed to connect device: {connectionResult.Error}");
                return;
            }

            _cts = new CancellationTokenSource();
            Console.WriteLine("Starting to listen for messages...");

            await _deviceManager.ReceiveCloudToDeviceMessagesAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not start listening to messages. {ex.Message}");
        }
    }

    private void UpdateToggleButtonText()
    {
        ToggleButtonText = DeviceState == "Off" ? "Off" : "On";
    }

    public void UpdateDeviceState(string newState)
    {
        try
        {
            Console.WriteLine("Updating device state..");
            DeviceState = newState;
            UpdateToggleButtonText();

            bool isDeviceOn = newState == "On";
            _deviceManager.UpdateDeviceTwinAsync(isDeviceOn).ConfigureAwait(false);
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
