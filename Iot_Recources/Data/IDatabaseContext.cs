using Iot_Recources.Models;

namespace Iot_Recources.Data;

public interface IDatabaseContext
{
    Task<ResponseResult<DeviceSettings>> GetSettingsAsync();
    Task<ResponseResult> ResetSettingsAsync();
    Task<ResponseResult> SaveSettingsAsync(DeviceSettings settings);

}
