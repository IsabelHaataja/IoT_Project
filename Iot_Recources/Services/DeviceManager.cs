using Iot_Recources.Factories;
using Iot_Recources.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Iot_Recources.Services;

public class DeviceManager : IDeviceManager
{
    // add public connectionstring to add to constuctor in App.xaml
    private string _iotHubConnectionString;
    private DeviceClient? _client;
    private RegistryManager? _registryManager;

    public DeviceManager(string iotHubConnectionstring)
    {
        _iotHubConnectionString = iotHubConnectionstring;
    }
    // Register a new device and return the connection string
    public async Task<ResponseResult<DeviceConnectionString>> RegisterDeviceAsync(string deviceId)
    {
        try
        {
            Debug.WriteLine($"Using IoT Hub Connection String: {_iotHubConnectionString}");

            var hostName = _iotHubConnectionString.Split(';')
                .FirstOrDefault(part => part.StartsWith("HostName=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=')[1];

            if (hostName == null)
            {
                return ResponseResultFactory.Error<DeviceConnectionString>("Invalid IoT Hub connection string. HostName not found.");
            }
 
            _registryManager = RegistryManager.CreateFromConnectionString(_iotHubConnectionString);

            var existingDevice = await _registryManager.GetDeviceAsync(deviceId);
            if (existingDevice != null)
            {
                return ResponseResultFactory.Error<DeviceConnectionString>($"Device '{deviceId}' already exists.");
            }

            // Register the device
            var newDevice = new Device(deviceId);
            var device = await _registryManager.AddDeviceAsync(newDevice);

            var deviceConnectionString = new DeviceConnectionString
            {
                HostName = hostName,           
                DeviceId = device.Id,        
                SharedAccessKey = device.Authentication.SymmetricKey.PrimaryKey
            };

            Debug.WriteLine($"Device registered successfully. Connection String: {deviceConnectionString.ConnectionString}");

            return ResponseResultFactory.Success(deviceConnectionString);
        }
        catch (Exception ex)
        {

            return ResponseResultFactory.Error<DeviceConnectionString>($"Device registration failed: {ex.Message}");
        }
    }
    public async Task<ResponseResult> ConnectToIotHubAsync(DeviceConnectionString deviceConnectionString)
    {
        try
        {
            Debug.WriteLine($"Connecting to IoT Hub using connection string: {deviceConnectionString.ConnectionString}");

            _client = DeviceClient.CreateFromConnectionString(deviceConnectionString.ConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);

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

    public async Task ReceiveCloudToDeviceMessagesAsync()
    {
        if (_client == null)
        {
            Debug.WriteLine("Device client not initialized.");
            return;
        }

        Debug.WriteLine("Listening for cloud-to-device messages...");

        while (true)
        {
            try
            {
                // Receive the message from the cloud
                var receivedMessage = await _client.ReceiveAsync();

                if (receivedMessage == null) continue;

                var messageContent = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Debug.WriteLine($"Received message: {messageContent}");

                // Process the message and determine if it's a command (e.g., turn on/off)
                if (messageContent.Contains("TurnOn"))
                {
                    await UpdateDeviceTwinAsync(true);
                }
                else if (messageContent.Contains("TurnOff"))
                {
                    await UpdateDeviceTwinAsync(false);
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
    public async Task UpdateDeviceTwinAsync(bool isDeviceOn)
    {
        try
        {
            var twinCollection = new TwinCollection();
            twinCollection["isDeviceOn"] = isDeviceOn; // Update the reported properties

            await _client.UpdateReportedPropertiesAsync(twinCollection);

            Debug.WriteLine($"Device twin updated: isDeviceOn = {isDeviceOn}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating device twin: {ex.Message}");
        }
    }

}
