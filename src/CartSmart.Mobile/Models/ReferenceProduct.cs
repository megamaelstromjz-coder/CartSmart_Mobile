using SQLite;

namespace CartSmart.Mobile.Models;

/// <summary>Cached row for `reference_products` (spec Section 4) — powers offline autocomplete (FE-1.4).</summary>
[Table("reference_products")]
public class ReferenceProduct
{
    [PrimaryKey]
    public string ProductId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? DefaultUnit { get; set; }

    public int SchemaVersion { get; set; } = 1;
}
