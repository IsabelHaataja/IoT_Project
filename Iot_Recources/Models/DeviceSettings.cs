
using SQLite;
using System.ComponentModel.DataAnnotations;

namespace Iot_Recources.Models;

public class DeviceSettings
{
    [Key]
    [PrimaryKey]
    public string Id { get; set; } = null!;
    public string? Type { get; set; }
    public string IotHubConnectionString { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;
    public string DeviceConnectionString { get; set; } = null!;
}
