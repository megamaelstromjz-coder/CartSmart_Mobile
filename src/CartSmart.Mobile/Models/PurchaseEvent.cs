using SQLite;

namespace CartSmart.Mobile.Models;

/// <summary>
/// Local-only purchase history feeding <c>PredictionService</c> (spec Section 4/FR-2.1).
/// Never synced to the API — hard constraint (NFR-2, FR-5.3).
/// </summary>
[Table("purchase_events")]
public class PurchaseEvent
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string ProductName { get; set; } = string.Empty;

    public DateTimeOffset PurchasedAt { get; set; }
    public double Quantity { get; set; }

    public int SchemaVersion { get; set; } = 1;
}
