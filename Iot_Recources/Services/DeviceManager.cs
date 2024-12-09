using Iot_Recources.Data;
using Iot_Recources.Factories;
using Iot_Recources.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
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
    //private DeviceTwinManager _deviceTwinManager;
    private readonly IDatabaseContext _context;
    private CancellationToken _ct;

    public event Action<string> OnDeviceStateChanged;

    public DeviceManager(IDatabaseContext context)
    {
        _context = context;
        _client = null;
    }

    public async Task PollDeviceTwinForChangesAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_client == null) throw new InvalidOperationException("Device client not initialized for polling.");

                var twin = await _client.GetTwinAsync();
                if (twin.Properties.Desired.Contains("deviceState"))
                {
                    var newState = twin.Properties.Desired["deviceState"].ToString();
                    OnDeviceStateChanged?.Invoke(newState);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error polling for device twin changes: {ex.Message}");
            }

            await Task.Delay(2000, ct);
        }
    }

    public async Task<ResponseResult<string>> RegisterDeviceAsync(string deviceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return ResponseResultFactory.Error<string>("DeviceId must not be null or empty.");
            }
            var iotHubConnectionString = await _context.GetIotHubConnectionStringAsync();

            Debug.WriteLine($"Using IoT Hub Connection String: {iotHubConnectionString}");

            var hostName = iotHubConnectionString.Split(';')
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

            _registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            var existingDevice = await _registryManager.GetDeviceAsync(deviceId);

            if (existingDevice != null)
            {
                return ResponseResultFactory.Error<string>($"Device '{deviceId}' already exists.");
            }
            
            var newDevice = new Device(deviceId);
            var device = await _registryManager.AddDeviceAsync(newDevice);

            var deviceConnectionString = $"HostName={hostName};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";

            Debug.WriteLine($"Device registered successfully. Connection String: {deviceConnectionString}");

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
            if (_client == null)
            {
                _client = DeviceClient.CreateFromConnectionString(deviceConnectionString);

                await _client.OpenAsync();

                Console.WriteLine("Device connected to IoT Hub.");

                return ResponseResultFactory.Success("IotHub connection succeeded.");
            }

            await PollDeviceTwinForChangesAsync(_ct);

            Console.WriteLine("DeviceClient is already initialized.");
            return ResponseResultFactory.Success("IotHub connection succeeded.");
        }
        catch (Exception ex)
        {
            return ResponseResultFactory.Error($"IotHub connection failed: {ex.Message}");
        }
    }
    public DeviceClient GetDeviceClient()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("DeviceClient is not initialized. Please connect first.");
        }
        return _client;
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
            Debug.WriteLine("Device client not initialized for C2D.");
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
                Console.WriteLine($"Received message: {messageContent}");

                if (messageContent.Contains("TurnOn")/* && OnDeviceStateChanged != null*/)
                {
                    await UpdateDeviceTwinAsync(true);
                    Console.WriteLine("OnDeviceStateChanged invoked.");
                    OnDeviceStateChanged.Invoke("On");
                }
                else if (messageContent.Contains("TurnOff"))
                {
                    await UpdateDeviceTwinAsync(false);
                    Console.WriteLine("OnDeviceStateChanged invoked.");
                    OnDeviceStateChanged.Invoke("Off");
                }

                await _client.CompleteAsync(receivedMessage);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Message listening canceled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error receiving C2D message: {ex.Message}");
            }

            await Task.Delay(1000);
        }
    }
    public async Task UpdateDeviceTwinAsync(bool isDeviceOn)
    {
        try
        {
            Console.WriteLine("Updating device twin...");

            if (_client == null)
                throw new InvalidOperationException("DeviceClient not initialized.");

            var twinCollection = new TwinCollection()
            {
                ["deviceState"] = isDeviceOn ? "On" : "Off"
            };

            Console.WriteLine("Attempting to update device twin...");
            await _client.UpdateReportedPropertiesAsync(twinCollection);

            Console.WriteLine($"Device twin updated: isDeviceOn = {isDeviceOn}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating device twin: {ex.Message}");
        }
    }
}
