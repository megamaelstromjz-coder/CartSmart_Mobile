using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Services;

/// <summary>
/// FR-2.x on-device prediction engine (rolling average / exponential smoothing over
/// <c>purchase_events</c>). Entirely local — nothing here is ever synced (FR-5.3).
/// </summary>
public interface IPredictionService
{
    /// <summary>Records a purchase (an item being checked off) and updates that product's rolling prediction.</summary>
    Task RecordPurchaseAsync(
        string productName, double quantity, string? unit, string? category,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SuggestionSignal>> GetSuggestionsAsync(CancellationToken cancellationToken = default);

    /// <summary>All tracked products, including ones with too little history to predict yet (History page).</summary>
    Task<IReadOnlyList<PredictionModelState>> GetHistoryAsync(CancellationToken cancellationToken = default);

    Task SnoozeAsync(string productName, TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>Skip this cycle's suggestion — pushes the predicted-need date out by another full interval.</summary>
    Task DismissAsync(string productName, CancellationToken cancellationToken = default);
}
