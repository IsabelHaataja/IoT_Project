
namespace Iot_Recources.Models;

public class DeviceConnectionString
{
    public string HostName { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public string SharedAccessKey { get; set; } = null!;
    public string ConnectionString => $"HostName={HostName};DeviceId={DeviceId};SharedAccessKey={SharedAccessKey}";
}
