using SQLite;

namespace CartSmart.Mobile.Models;

/// <summary>
/// Small key/value store for singleton local markers: the cached reference-data version
/// (Section 4) and the last server-issued sync cursor (Section 6.4 `serverTimestamp`).
/// </summary>
[Table("app_settings")]
public class AppSetting
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public static class Keys
    {
        public const string ReferenceVersion = "reference_version";
        public const string LastSyncCursor = "last_sync_cursor";
    }
}
