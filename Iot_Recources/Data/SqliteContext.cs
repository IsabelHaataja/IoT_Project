using Iot_Recources.Factories;
using Iot_Recources.Models;
using Microsoft.Extensions.Logging;
using SQLite;
using System.Diagnostics;
using System.Text;

namespace Iot_Recources.Data;

public class SqliteContext : IDatabaseContext
{
    private readonly ILogger<SqliteContext> _logger;
    private SQLiteAsyncConnection _context;
    private readonly string _deviceType = "AC";
    public SqliteContext(ILogger<SqliteContext> logger)
    {
        _logger = logger;

        string dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SmarthomeDatabase");


        // Construct the full path to the database file
        string dbPath = Path.Combine(dbFolder, "Smarthome_database.db3");
        Debug.WriteLine($"Database Path: {dbPath}");

        // Initialize the SQLite connection
        _context = new SQLiteAsyncConnection(dbPath);

        SetDeviceTypeAsync(_deviceType).ConfigureAwait(false);
    }

    public async Task<ResponseResult<DeviceSettings>> GetSettingsAsync()
    {
        try
        {
            var deviceSettings = (await _context!.Table<DeviceSettings>().ToListAsync()).SingleOrDefault();
            if (deviceSettings != null)
                return ResponseResultFactory.Success(deviceSettings);

            return ResponseResultFactory.Error<DeviceSettings>("No device settings were found.");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "An error occurred while getting device settings.");
            return ResponseResultFactory.Error<DeviceSettings>("An error occurred while getting device settings.");
        }
    }

    public async Task<ResponseResult<DeviceSettings>> SetDeviceTypeAsync(string deviceType)
    {
        try
        {
            var deviceSettings = (await _context!.Table<DeviceSettings>().ToListAsync()).SingleOrDefault();
            if (deviceSettings != null && deviceSettings.Type == null)
            {
                deviceSettings.Type = deviceType;
                await SaveSettingsAsync(deviceSettings);

                return ResponseResultFactory.Success(deviceSettings);
            }
            else
                return ResponseResultFactory.Error<DeviceSettings>("DeviceSettings type is already set.");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "An error occurred while setting device settings type.");
            return ResponseResultFactory.Error<DeviceSettings>("An error occurred while setting device settings type.");
        }
    }

    public async Task<ResponseResult> ResetSettingsAsync()
    {
        try
        {
            await _context!.DeleteAllAsync<DeviceSettings>();
            var deviceSettings = await _context.Table<DeviceSettings>().ToListAsync();
            if(deviceSettings.Count == 0)
                return ResponseResultFactory.Success("Settings were reset successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred settings could not be reset.");
            return ResponseResultFactory.Error("An error occurred Settings could not be reset.");
        }
        _logger.LogError("Settings could not be reset.");
        return ResponseResultFactory.Error("Settings could not be reset.");
    }

    public async Task<ResponseResult> SaveSettingsAsync(DeviceSettings settings)
    {
        try
        {
            if (!string.IsNullOrEmpty(settings.Id))
            {
                var response = await GetSettingsAsync();

                if (response.Result != null)
                {
                    response.Result.IotHubConnectionString = settings.IotHubConnectionString;
                    response.Result.Type = settings.Type;

                    await _context!.UpdateAsync(settings);
                    return ResponseResultFactory.Success("Settings were updated successfully");
                }
                else
                {
                    await _context!.InsertAsync(settings);
                    return ResponseResultFactory.Success("Settings were inserted successfully");
                }
            }

            _logger.LogError("Failed to save settings: device ID is null or empty.");
            return ResponseResultFactory.Error("Failed to save settings: device ID is null or empty.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings: device ID is null or empty.");
            return ResponseResultFactory.Error("Failed to save settings: device ID is null or empty.");
        }
    }
    
    public async Task<ResponseResult> SaveDeviceConnectionStringAsync(string connectionString)
    {
        try
        {
            var response = await GetSettingsAsync();

            if (response.Result != null)
            {
                response.Result.DeviceConnectionString = connectionString;
                await _context.UpdateAsync(response.Result);

                return ResponseResultFactory.Success("Device connection string updated successfully.");
            }
            else
            {
                return ResponseResultFactory.Error("No device settings found to update.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save device connection string.");
            return ResponseResultFactory.Error("Failed to save device connection string.");
        }
    }

    public async Task<string> GetIotHubConnectionStringAsync()
    {
        try
        {
            var response = await GetSettingsAsync();
            var connectionString = response.Result?.IotHubConnectionString ?? string.Empty;

            var hostName = connectionString.Split(';')
                .FirstOrDefault(part => part.StartsWith("HostName=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=')[1];

            // add .azure-devices.net if missing
            if (hostName != null && !hostName.EndsWith(".azure-devices.net"))
            {
                hostName += ".azure-devices.net";

                // Rebuild the connection string with the correct HostName
                connectionString = connectionString.Replace($"HostName={hostName.Substring(0, hostName.IndexOf('.'))}", $"HostName={hostName}");
            }

            Debug.WriteLine($"Got iothub connection string {connectionString}");

            return connectionString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting the connection string.");
            return string.Empty;
        }
    }
    public async Task<string> GetDeviceConnectionStringAsync()
    {
        try
        {
            var response = await GetSettingsAsync();
            var connectionString = response.Result?.DeviceConnectionString ?? string.Empty;

            Debug.WriteLine($"Got Device connection string {connectionString}");
            return connectionString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting the device connection string.");
            return string.Empty;
        }
    }
}
