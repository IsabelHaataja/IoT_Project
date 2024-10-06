using Iot_Recources.Models;

namespace Iot_Recources.Services
{
    public interface IDeviceManager
    {
        Task DisconnectAsync(CancellationToken ct);
        Task<ResponseResult<string>> SendDataAsync(string content, CancellationToken ct);
    }
}