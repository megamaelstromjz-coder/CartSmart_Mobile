using SQLite;

namespace CartSmart.Mobile.Models;

/// <summary>Local row for the `lists` table (spec Section 4).</summary>
[Table("lists")]
public class ShoppingList
{
    [PrimaryKey]
    public string ListId { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;

    /// <summary>Pending push to the API — set on any local create/rename, cleared on successful sync.</summary>
    public bool IsDirty { get; set; }

    /// <summary>Device that made the last local edit (Section 7 offline queue attribution).</summary>
    public string? ClientDeviceId { get; set; }

    /// <summary>`updatedAt` last seen from the server (PUT response or /sync pull) — used for conflict detection (FE-6.3).</summary>
    public DateTimeOffset? ServerUpdatedAt { get; set; }

    public int SchemaVersion { get; set; } = 1;

    public bool IsDeleted { get; set; }
}
