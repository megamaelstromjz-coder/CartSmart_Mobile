using SQLite;

namespace CartSmart.Mobile.Models;

/// <summary>Local row for the `list_items` table (spec Section 4).</summary>
[Table("list_items")]
public class ListItem
{
    [PrimaryKey]
    public string ItemId { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    public string ListId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Category { get; set; }
    public bool IsChecked { get; set; }

    public bool IsDirty { get; set; }
    public string? ClientDeviceId { get; set; }
    public DateTimeOffset? ServerUpdatedAt { get; set; }
    public int SchemaVersion { get; set; } = 1;

    public bool IsDeleted { get; set; }
}
