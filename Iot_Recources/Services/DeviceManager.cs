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
    private DeviceClient _client;
    private RegistryManager? _registryManager;
    private DeviceTwinManager _deviceTwinManager;
    private readonly IDatabaseContext _context;
    private CancellationToken _ct;

    public event Action<string> OnDeviceStateChanged;

    public DeviceManager(IDatabaseContext context, DeviceClient client)
    {
        _context = context;
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task PollDeviceTwinForChangesAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_client == null) throw new InvalidOperationException("Device client not initialized.");

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
    public async Task InitializeDeviceClientAsync()
    {
        try
        {
            if (_client == null)
            {

                _deviceConnectionString = await _context.GetDeviceConnectionStringAsync();

                if (string.IsNullOrEmpty(_deviceConnectionString))
                {
                    throw new InvalidOperationException("IoT Hub connection string cannot be null or empty.");
                }
            }

            _iotHubConnectionString = await _context.GetIotHubConnectionStringAsync();

            _registryManager = RegistryManager.CreateFromConnectionString(_iotHubConnectionString);

            _deviceTwinManager = new DeviceTwinManager(_context, _client);
            
            //await _deviceTwinManager.InitializeAsync();

            await PollDeviceTwinForChangesAsync(_ct);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
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
                Console.WriteLine($"Received message: {messageContent}");

                if (messageContent.Contains("TurnOn") && OnDeviceStateChanged != null)
                {
                    await _deviceTwinManager.UpdateDeviceTwinAsync(true);
                    Console.WriteLine("OnDeviceStateChanged invoked.");
                    OnDeviceStateChanged.Invoke("On");
                }
                else if (messageContent.Contains("TurnOff") && OnDeviceStateChanged != null)
                {
                    await _deviceTwinManager.UpdateDeviceTwinAsync(false);
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
}
