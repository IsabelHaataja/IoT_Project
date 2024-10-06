using Iot_Recources.Models;
using Microsoft.Azure.Devices.Client;
using System.Text;

namespace Iot_Recources.Services;

public class DeviceManager : IDeviceManager
{
    // add public connectionstring to add to constuctor in App.xaml

    private readonly string _connectionstring;
    private readonly DeviceClient _client;

    public DeviceManager(string connectionstring)
    {
        _connectionstring = connectionstring;
        _client = DeviceClient.CreateFromConnectionString(connectionstring);
    }

    public async Task<ResponseResult<string>> SendDataAsync(string content, CancellationToken ct)
    {
        try
        {
            using var message = new Message(Encoding.UTF8.GetBytes(content))
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };

            await _client.SendEventAsync(message, ct);
            return new ResponseResult<string> { Succeeded = true };
        }
        catch (OperationCanceledException)
        {
            return new ResponseResult<string> { Succeeded = false, Error = "Operation was canceled." };
        }
        catch (Exception ex)
        {
            return new ResponseResult<string> { Succeeded = false, Error = ex.Message };
        }
    }

    public async Task DisconnectAsync(CancellationToken ct)
    {
        await _client.CloseAsync(ct);
    }
}
