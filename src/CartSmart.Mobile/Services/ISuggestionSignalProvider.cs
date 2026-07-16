namespace CartSmart.Mobile.Services;

public record SuggestionSignal(string ProductName, double Confidence, DateTimeOffset PredictedNeedBy);

/// <summary>
/// Pluggable ranking-signal boundary (spec Section 3/7.1, Section 10.1). Phase 1's only
/// implementation is on-device statistics; Phase 2 Option A (server cold-start priors) plugs
/// in a second implementation here without <c>PredictionService</c> itself changing.
/// </summary>
public interface ISuggestionSignalProvider
{
    Task<IReadOnlyList<SuggestionSignal>> GetSignalsAsync(CancellationToken cancellationToken = default);
}
