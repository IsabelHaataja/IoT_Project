using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot_Recources.Services
{
    public class DeviceTwinManager
    {
        // TODO retreive device's primary connectionstring from its IotHub
        private readonly DeviceClient _client = DeviceClient.CreateFromConnectionString("", TransportType.Mqtt);

    
        // TODO - implement (usage) -->
        // while(true)
        // {
        //     var json = JsonConvert.SerializeObject(new DeviceConfigInfo()));
        //     await SendDataAsync(json);
        //     Console.WriteLine($"Message was sent: {json}");
        //     await Task.Delay(60 * 1000);
        // }
        public async Task SendDataAsync(string content)
        {
            using var message = new Message(Encoding.UTF8.GetBytes(content))
            {
                ContentType = "appliccation/json",
                ContentEncoding = "utf-8"
            };

            await _client.SendEventAsync(message);
        }
    }
}
