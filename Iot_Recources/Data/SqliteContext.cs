using Iot_Recources.Factories;
using Iot_Recources.Models;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Iot_Recources.Data;

public class SqliteContext : IDatabaseContext
{
    private readonly ILogger<SqliteContext> _logger;
    private readonly SQLiteAsyncConnection? _context;
    public SqliteContext(ILogger<SqliteContext> logger, Func<string> directoryPath, string databaseName = "iot_device_database.db3")
    {
        _logger = logger;

        try
        {
            var databsePath = Path.Combine(directoryPath(), databaseName);
            if (string.IsNullOrWhiteSpace(databsePath))
                throw new ArgumentException("The database path cannot be null or empty.");
            
            _context = new SQLiteAsyncConnection(databsePath);

            CreateTablesAsync().ConfigureAwait(false);
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex.Message, "An error occurred while creating database connection.");
        }
    }
    public async Task CreateTablesAsync()
    {
        try
        {
            if (_context == null)
                throw new ArgumentException("The database has not been initialized.");

            await _context.CreateTableAsync<DeviceSettings>();

            _logger.LogInformation("Database tables were created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "An error occurred while creating database tables.");
        }
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
            _logger.LogError(ex, "An error occurred while getting device settings.");
            return ResponseResultFactory.Error<DeviceSettings>("An error occurred while getting device settings.");
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
                    response.Result.Location = settings.Location;
                    response.Result.ConnectionString = settings.ConnectionString;
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
}
