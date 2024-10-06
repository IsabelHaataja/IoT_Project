using System.Text;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Functions;

public class ViewIotMessages
{
    private readonly ILogger<ViewIotMessages> _logger;

    public ViewIotMessages(ILogger<ViewIotMessages> logger)
    {
        _logger = logger;
    }

    //TODO - swap "iot-unit" to eventhub compatible endpoint from Azure
    [Function(nameof(ViewIotMessages))]
    public void Run([EventHubTrigger("iot-unit", Connection = "IotHubEndpoint")] EventData[] events)
    {
        foreach (EventData @event in events)
        {
            string messageBody = Encoding.UTF8.GetString(@event.Body.ToArray());
            _logger.LogInformation("Event Body: {body}", messageBody);
        }
    }
}
