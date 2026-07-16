using SQLite;

namespace CartSmart.Mobile.Models;

/// <summary>Single-row table holding this device's own registration (spec Section 4).</summary>
[Table("device_registration")]
public class DeviceRegistration
{
    [PrimaryKey]
    public string ClientDeviceId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Server-assigned id returned by POST /devices, once registered.</summary>
    public string? DeviceId { get; set; }

    public string Platform { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTimeOffset? RegisteredAt { get; set; }

    public int SchemaVersion { get; set; } = 1;
}
