
namespace Iot_Recources.Services
{
    public interface IDeviceTwinManager
    {
        Task InitializeAsync();
        Task SendDataAsync(string content);
        Task StartSendingDataAsync();
        Task UpdateDeviceTwinAsync(bool isDeviceOn);
    }
}