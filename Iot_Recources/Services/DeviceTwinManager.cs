using Iot_Recources.Data;
using Iot_Recources.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace Iot_Recources.Services;

public class DeviceTwinManager
{
    private static DeviceClient _client;
    private readonly IDatabaseContext _context;

    public DeviceTwinManager(IDatabaseContext context)
    {
        _context = context;
    }

    public async Task InitializeDeviceClientAsync()
    {
        var response = await _context.GetSettingsAsync();

        if (response.Succeeded && response.Result != null)
        {
            string connectionString = response.Result.DeviceConnectionString;

            if (!string.IsNullOrEmpty(connectionString))
            {       
                _client = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
                Debug.WriteLine("Device client initialized successfully.");
            }
            else
            {
                Debug.WriteLine("Error: IoT Hub connection string is empty.");
            }
        }
        else
        {
            Debug.WriteLine("Error: Unable to retrieve device settings.");
        }
    }
    
    public async Task StartSendingDataAsync()
    {
        while (true)
        {
            var json = JsonConvert.SerializeObject(new DeviceConfigInfo());
            await SendDataAsync(json);
            Console.WriteLine($"Message was sent: {json}");
            await Task.Delay(60 * 1000);
        }
    }


    public async Task SendDataAsync(string content)
    {
        using var message = new Message(Encoding.UTF8.GetBytes(content))
        {
            ContentType = "appliccation/json",
            ContentEncoding = "utf-8"
        };

        try
        {
            await _client.SendEventAsync(message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error sending data: {ex.Message}");
        }
    }

    public async Task UpdateDeviceTwinAsync(bool isDeviceOn)
    {
        try
        {
            var twinCollection = new TwinCollection();
            twinCollection["isDeviceOn"] = isDeviceOn;

            await _client.UpdateReportedPropertiesAsync(twinCollection);

            Debug.WriteLine($"Device twin updated: isDeviceOn = {isDeviceOn}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating device twin: {ex.Message}");
        }
    }
}
