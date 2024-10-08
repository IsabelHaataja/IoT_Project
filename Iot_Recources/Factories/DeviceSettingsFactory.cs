using Iot_Recources.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot_Recources.Factories;

public static class DeviceSettingsFactory
{
    public static DeviceSettings Create()
    {
        return new DeviceSettings()
        {
            Id = Guid.NewGuid().ToString()
        };
    }

    public static DeviceSettings Create(string id)
    {
        return new DeviceSettings()
        {
            Id = id
        };
    }
    public static DeviceSettings Create(string id, string? location, string? type, string? connectionString)
    {
        return new DeviceSettings()
        {
            Id = id,
            Location = location,
            Type = type,
            ConnectionString = connectionString
        };
    }
}
