using Iot_Recources.Models;

namespace Iot_Recources.Services;

public interface IDeviceManager
{
    Task<ResponseResult<DeviceConnectionString>> RegisterDeviceAsync(string deviceId);
    Task<ResponseResult> DisconnectAsync(CancellationToken ct);
    Task<ResponseResult<string>> SendDataAsync(string content, CancellationToken ct);
    Task<ResponseResult> ConnectToIotHubAsync(DeviceConnectionString deviceConnectionString);
}