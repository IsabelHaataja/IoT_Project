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

        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Smarthome_database.db3");
        Debug.WriteLine($"Database Path: {Path.GetFullPath(dbPath)}");

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
            if (deviceSettings != null && deviceSettings!.Type == null)
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

    public async Task<string> GetDeviceConnectionStringAsync()
    {
        try
        {
            var response = await GetSettingsAsync();
            Debug.WriteLine($"Got Device connection string {response.Result?.IotHubConnectionString}");

            return response.Result?.IotHubConnectionString ?? string.Empty;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting the connection string.");
            return string.Empty;
        }
    }
}
