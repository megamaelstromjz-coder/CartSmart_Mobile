using CartSmart.Mobile.Data.Repositories;
using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Services;

public class PredictionService(
    IPredictionModelStateRepository modelStateRepository,
    IPurchaseEventRepository purchaseEventRepository) : IPredictionService, ISuggestionSignalProvider
{
    /// <summary>Exponential-smoothing weight blending a new purchase gap into the rolling interval.</summary>
    private const double SmoothingAlpha = 0.3;

    /// <summary>A product needs at least this many purchases before an interval can be predicted at all.</summary>
    public const int MinPurchasesForSuggestion = 2;

    /// <summary>Purchase count at/above which a prediction is "High confidence" rather than "Building data".</summary>
    public const int HighConfidencePurchaseCount = 4;

    public async Task RecordPurchaseAsync(
        string productName, double quantity, string? unit, string? category,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        await purchaseEventRepository.InsertAsync(new PurchaseEvent
        {
            ProductName = productName,
            PurchasedAt = now,
            Quantity = quantity,
            Unit = unit,
            Category = category,
        });

        var existing = await modelStateRepository.GetByProductNameAsync(productName);
        var purchaseCount = (existing?.PurchaseCount ?? 0) + 1;

        var intervalDays = existing is { PurchaseCount: > 0 }
            ? Blend(existing.IntervalDays, (now - existing.LastPurchasedAt).TotalDays, purchaseCount)
            : 0;

        await modelStateRepository.UpsertAsync(new PredictionModelState
        {
            ProductName = productName,
            IntervalDays = intervalDays,
            Weight = Confidence(purchaseCount),
            LastUpdatedAt = now,
            PurchaseCount = purchaseCount,
            LastPurchasedAt = now,
            Category = category ?? existing?.Category,
            Unit = unit ?? existing?.Unit,
            LastQuantity = quantity,
            SnoozedUntil = null,
        });
    }

    public async Task<IReadOnlyList<SuggestionSignal>> GetSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var states = await modelStateRepository.GetAllAsync();
        return states
            .Where(s => s.PurchaseCount >= MinPurchasesForSuggestion)
            .Select(ToSignal)
            .OrderBy(s => s.PredictedNeedBy)
            .ToList();
    }

    public Task<IReadOnlyList<SuggestionSignal>> GetSignalsAsync(CancellationToken cancellationToken = default)
        => GetSuggestionsAsync(cancellationToken);

    public async Task<IReadOnlyList<PredictionModelState>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var states = await modelStateRepository.GetAllAsync();
        return states.OrderByDescending(s => s.LastPurchasedAt).ToList();
    }

    public async Task SnoozeAsync(string productName, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var state = await modelStateRepository.GetByProductNameAsync(productName);
        if (state is null)
        {
            return;
        }

        state.SnoozedUntil = DateTimeOffset.UtcNow.Add(duration);
        await modelStateRepository.UpsertAsync(state);
    }

    public async Task DismissAsync(string productName, CancellationToken cancellationToken = default)
    {
        var state = await modelStateRepository.GetByProductNameAsync(productName);
        if (state is null)
        {
            return;
        }

        state.SnoozedUntil = state.LastPurchasedAt.AddDays(state.IntervalDays * 2);
        await modelStateRepository.UpsertAsync(state);
    }

    private static SuggestionSignal ToSignal(PredictionModelState state)
    {
        var predictedNeedBy = state.LastPurchasedAt.AddDays(state.IntervalDays);
        if (state.SnoozedUntil is { } snoozedUntil && snoozedUntil > predictedNeedBy)
        {
            predictedNeedBy = snoozedUntil;
        }

        return new SuggestionSignal(
            state.ProductName,
            state.Category,
            state.Weight,
            state.IntervalDays,
            state.LastPurchasedAt,
            predictedNeedBy);
    }

    private static double Blend(double currentIntervalDays, double latestGapDays, int purchaseCount)
        => purchaseCount == 2
            ? latestGapDays
            : (SmoothingAlpha * latestGapDays) + ((1 - SmoothingAlpha) * currentIntervalDays);

    private static double Confidence(int purchaseCount)
        => Math.Clamp((purchaseCount - 1) / (double)(HighConfidencePurchaseCount - 1), 0, 1);
}
