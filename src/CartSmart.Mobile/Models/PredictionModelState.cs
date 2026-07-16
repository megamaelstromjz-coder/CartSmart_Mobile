using SQLite;

namespace CartSmart.Mobile.Models;

/// <summary>
/// Local-only prediction model state — rolling-average/exponential-smoothing intervals and weights
/// per product (spec Section 4/FR-2.2). Never synced; not even required to sync (FR-5.3).
/// </summary>
[Table("model_state")]
public class PredictionModelState
{
    [PrimaryKey]
    public string ProductName { get; set; } = string.Empty;

    public double IntervalDays { get; set; }

    /// <summary>Prediction confidence, 0.0-1.0, derived from <see cref="PurchaseCount"/>.</summary>
    public double Weight { get; set; }

    public DateTimeOffset LastUpdatedAt { get; set; }

    public int PurchaseCount { get; set; }
    public DateTimeOffset LastPurchasedAt { get; set; }
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public double LastQuantity { get; set; }

    /// <summary>Set by Snooze/Dismiss in Suggestions (FE-2.x) to push the next predicted-need date out.</summary>
    public DateTimeOffset? SnoozedUntil { get; set; }

    public int SchemaVersion { get; set; } = 1;
}
