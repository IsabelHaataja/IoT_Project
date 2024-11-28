using Iot_Recources.Data;
using Iot_Recources.Factories;
using Iot_Recources.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace Iot_Recources.Services;

public class DeviceManager : IDeviceManager
{
    private string _iotHubConnectionString;
    private string _deviceConnectionString;
    private DeviceClient? _client;
    private RegistryManager? _registryManager;
    private DeviceTwinManager _deviceTwinManager;
    private readonly IDatabaseContext _context;

    public event Action<string> OnDeviceStateChanged;

    // tog bort string iotHubConnectionString från ctor

    public DeviceManager(IDatabaseContext context)
    {
        _context = context;
    }
    public async Task PollDeviceTwinForChangesAsync()
    {
        try
        {
            while (true)
            {
                if (_client == null)
                {
                    Debug.WriteLine("Device client not initialized.");
                    return;
                }

                // Get the latest twin
                var twin = await _client.GetTwinAsync();
                var updatedProperties = twin.Properties.Desired;

                // Check if the property has changed
                if (updatedProperties.Contains("isDeviceOn"))
                {
                    var newState = updatedProperties["isDeviceOn"].ToString();
                    // Notify the ViewModel that the state has changed
                    OnDeviceStateChanged?.Invoke(newState);
                }

                // Sleep for a period before checking again
                await Task.Delay(5000); // Check every 5 seconds (adjust as needed)
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error polling for device twin changes: {ex.Message}");
        }

    }
    public async Task InitializeAsync()
    {
        try
        {
            _deviceConnectionString = await _context.GetDeviceConnectionStringAsync();

            if (string.IsNullOrEmpty(_deviceConnectionString))
            {
                throw new InvalidOperationException("IoT Hub connection string cannot be null or empty.");
            }

            _client = DeviceClient.CreateFromConnectionString(_deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);

            _registryManager = RegistryManager.CreateFromConnectionString(_deviceConnectionString);

            _deviceTwinManager = new DeviceTwinManager(_context);
            await _deviceTwinManager.InitializeAsync();
        }
        catch (Exception ex) 
        {
            Debug.WriteLine(ex);
        }

    }

    public DeviceClient? GetDeviceClient()
    {
        return _client;
    }

    public async Task<ResponseResult<string>> RegisterDeviceAsync(string deviceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return ResponseResultFactory.Error<string>("DeviceId must not be null or empty.");
            }

            Debug.WriteLine($"Using IoT Hub Connection String: {_iotHubConnectionString}");

            var hostName = _iotHubConnectionString.Split(';')
                .FirstOrDefault(part => part.StartsWith("HostName=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=')[1];

            if (!hostName.EndsWith(".azure-devices.net"))
            {
                hostName += ".azure-devices.net";
            }

            Debug.WriteLine($"Using IoT Hub Connection String with: {hostName}");

            if (hostName == null)
            {
                return ResponseResultFactory.Error<string>("Invalid IoT Hub connection string. HostName not found.");
            }

            _registryManager = RegistryManager.CreateFromConnectionString(_iotHubConnectionString);
            var existingDevice = await _registryManager.GetDeviceAsync(deviceId);

            if (existingDevice != null)
            {
                return ResponseResultFactory.Error<string>($"Device '{deviceId}' already exists.");
            }
            
            // Register the device
            var newDevice = new Device(deviceId);
            var device = await _registryManager.AddDeviceAsync(newDevice);

            var deviceConnectionString = $"HostName={hostName};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";

            Debug.WriteLine($"Device registered successfully. Connection String: {deviceConnectionString}");
            // Save the device connection string to DeviceSettings in the database
            var dbResponse = await _context.SaveDeviceConnectionStringAsync(deviceConnectionString);

            if (!dbResponse.Succeeded)
            {
                return ResponseResultFactory.Error<string>($"Failed to save device connection string: {dbResponse}");
            }

            return ResponseResultFactory.Success(deviceConnectionString);
        }
        catch (Exception ex)
        {

            return ResponseResultFactory.Error<string>($"Device registration failed: {ex.Message}");
        }
    }
    public async Task<ResponseResult> ConnectToIotHubAsync(string deviceConnectionString)
    {
        try
        {
            Debug.WriteLine($"Connecting to IoT Hub using connection string: {deviceConnectionString}");

            _client = DeviceClient.CreateFromConnectionString(deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);

            await _client.OpenAsync();

            return ResponseResultFactory.Success("IotHub connection succeeded.");
        }
        catch (Exception ex)
        {
            return ResponseResultFactory.Error($"IotHub connection failed: {ex}");
        }
    }
    public async Task<ResponseResult<string>> SendDataAsync(string content, CancellationToken ct)
    {
        if (_client == null)
        {
            return ResponseResultFactory.Error<string>("Device is not connected to IoT Hub.");
        }

        try
        {
            using var message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(content))
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };

            await _client.SendEventAsync(message, ct);
            return ResponseResultFactory.Success("Message sent successfully.");
        }
        catch (OperationCanceledException)
        {
            return ResponseResultFactory.Error<string>("Operation was canceled.");
        }
        catch (Exception ex)
        {
            return ResponseResultFactory.Error<string>($"Error sending data: {ex.Message}");
        }
    }

    public async Task<ResponseResult> DisconnectAsync(CancellationToken ct)
    {
        if (_client != null)
        {
            try
            {
                await _client.CloseAsync(ct);
                _client.Dispose();
                _client = null;

                return ResponseResultFactory.Success("Device client disconnected.");
            }
            catch (Exception ex)
            {
                return ResponseResultFactory.Error($"Error while disconnecting: {ex.Message}");
            }
        }

        return ResponseResultFactory.Error("Could not disconnect device client.");
    }

    public async Task ReceiveCloudToDeviceMessagesAsync(CancellationToken ct)
    {
        if (_client == null)
        {
            // TODO - initialize device client if not initialized
            Debug.WriteLine("Device client not initialized.");
            return;
        }

        Debug.WriteLine("Listening for cloud-to-device messages...");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var receivedMessage = await _client.ReceiveAsync();

                if (receivedMessage == null) continue;

                var messageContent = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Debug.WriteLine($"Received message: {messageContent}");

                if (messageContent.Contains("TurnOn"))
                {
                    await _deviceTwinManager.UpdateDeviceTwinAsync(true);
                    OnDeviceStateChanged?.Invoke("On");
                }
                else if (messageContent.Contains("TurnOff"))
                {
                    await _deviceTwinManager.UpdateDeviceTwinAsync(false);
                    OnDeviceStateChanged?.Invoke("Off");
                }

                // Complete the message after processing it
                await _client.CompleteAsync(receivedMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error receiving C2D message: {ex.Message}");
            }

            await Task.Delay(1000); // Poll every second
        }
    }

    public async Task SetUpDirectMethodHandlersAsync()
    {
        try
        {
            if (_client == null)
            {
                Debug.WriteLine("Device client not initialized.");
                return;
            }

            await _client.SetMethodHandlerAsync("TurnOn", TurnOnAsync, null);

            await _client.SetMethodHandlerAsync("TurnOff", TurnOffAsync, null);

            Debug.WriteLine("Direct method handlers set up.");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Direct method handler could not be set up. {e.Message}");
        }
    }

    private async Task<MethodResponse> TurnOnAsync(MethodRequest methodRequest, object userContext)
    {
        try
        {
            if (_deviceTwinManager == null)
            {
                Debug.WriteLine("DeviceTwinManager is not initialized.");
                return new MethodResponse(Encoding.UTF8.GetBytes("DeviceTwinManager is not initialized."), 500);
            }

            Debug.WriteLine("TurnOn method invoked.");

            await _deviceTwinManager.UpdateDeviceTwinAsync(true); // true indicates "on"

            var result = new { message = "Device turned on successfully" };
            var jsonResponse = JsonConvert.SerializeObject(result);

            return new MethodResponse(Encoding.UTF8.GetBytes(jsonResponse), 200);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Device could not be turned on.{ex.Message}");
            return new MethodResponse(500);
        }
    }

    private async Task<MethodResponse> TurnOffAsync(MethodRequest methodRequest, object userContext)
    {
        try
        {
            if (_deviceTwinManager == null)
            {
                Debug.WriteLine("DeviceTwinManager is not initialized.");
                return new MethodResponse(Encoding.UTF8.GetBytes("DeviceTwinManager is not initialized."), 500);
            }

            Debug.WriteLine("TurnOff method invoked.");

            await _deviceTwinManager.UpdateDeviceTwinAsync(false); // false indicates "off"

            var result = new { message = "Device turned off successfully" };
            var jsonResponse = JsonConvert.SerializeObject(result);

            return new MethodResponse(Encoding.UTF8.GetBytes(jsonResponse), 200);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Device could not be turned off.{ex.Message}");
            return new MethodResponse(500);
        }
    }
}
