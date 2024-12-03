using Iot_Recources.Data;
using Iot_Recources.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace Iot_Recources.Services;

public class DeviceTwinManager : IDeviceTwinManager
{
    private readonly DeviceClient _client;
    private DeviceManager _deviceManager;
    private readonly IDatabaseContext _context;
    private CancellationToken _ct;

    public DeviceTwinManager(IDatabaseContext context, DeviceClient client)
    {
        _context = context;
        _client = client ?? throw new ArgumentNullException(nameof(client));

    }
    public async Task InitializeAsync()
    {
        try
        {
            if (_client == null)
            {
                Console.WriteLine("Device client is not initialized.");
                return;
            }

            Debug.WriteLine("Device client initialized and ready for twin operations.");

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public async Task UpdateDeviceTwinAsync(bool isDeviceOn)
    {
        try
        {
            Console.WriteLine("Updating device twin...");
            if (_client == null)
            {
                Debug.WriteLine("Device client is not initialized.");
                return;
            }

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

    public async Task CloseAsync()
    {
        await _client.CloseAsync();
    }
}
