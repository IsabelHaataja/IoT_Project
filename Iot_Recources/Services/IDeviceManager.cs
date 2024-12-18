﻿using Iot_Recources.Models;
using Microsoft.Azure.Devices.Client;

namespace Iot_Recources.Services;

public interface IDeviceManager
{
    Task PollDeviceTwinForChangesAsync(CancellationToken ct);
    Task<ResponseResult<string>> RegisterDeviceAsync(string deviceId);
    Task<ResponseResult> DisconnectAsync(CancellationToken ct);
    Task<ResponseResult<string>> SendDataAsync(string content, CancellationToken ct);
    Task<ResponseResult> ConnectToIotHubAsync(string deviceConnectionString);
    Task ReceiveCloudToDeviceMessagesAsync(CancellationToken ct);
    DeviceClient GetDeviceClient();
    event Action<string> OnDeviceStateChanged;
    Task UpdateDeviceTwinAsync(bool isDeviceOn);
}