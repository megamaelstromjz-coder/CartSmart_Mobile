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
    public double Weight { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }

    public int SchemaVersion { get; set; } = 1;
}
