
using SQLite;
using System.ComponentModel.DataAnnotations;

namespace Iot_Recources.Models;

public class DeviceSettings
{
    [Key]
    [PrimaryKey]
    public string Id { get; set; } = null!;
    public string? Location { get; set; }
    public string? Type { get; set; }
    public string? ConnectionString { get; set; }
}
