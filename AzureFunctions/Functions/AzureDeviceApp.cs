using Iot_Recources.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace AzureFunctions.Functions;

public class AzureDeviceApp
{
    //private readonly ILogger _logger;
    //// TODO - add device's primary connectionstring from Azure
    //private readonly DeviceClient client = DeviceClient.CreateFromConnectionString("");

    //public AzureDeviceApp(ILoggerFactory loggerFactory)
    //{
    //    _logger = loggerFactory.CreateLogger<AzureDeviceApp>();
    //}

    //[Function("AzureDeviceApp")]
    //public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    //{
    //    var json = JsonConvert.SerializeObject(new DeviceConfigInfo());
    //        await SendDataAsync(json);

    //    _logger.LogInformation($"Message sent: {json}");
    //}

    //public async Task SendDataAsync(string content)
    //{
    //    using var message = new Message(Encoding.UTF8.GetBytes(content))
    //    {
    //        ContentType = "application/json",
    //        ContentEncoding = "utf-8"
    //    };

    //    await client.SendEventAsync(message);
    //}
}
