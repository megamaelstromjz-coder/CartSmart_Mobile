namespace CartSmart.Mobile.Services;

/// <summary>
/// FR-2.x on-device prediction engine (rolling average / exponential smoothing over
/// <c>purchase_events</c>). Out of scope for this pass — Section 5.2 (List Management) is the
/// only fully-implemented vertical slice; this stub exists so DI wiring and the
/// <see cref="ISuggestionSignalProvider"/> boundary (Section 10.1) are in place for the next pass.
/// </summary>
public interface IPredictionService
{
    Task<IReadOnlyList<SuggestionSignal>> GetSuggestionsAsync(CancellationToken cancellationToken = default);
}

public class PredictionService : IPredictionService, ISuggestionSignalProvider
{
    public Task<IReadOnlyList<SuggestionSignal>> GetSuggestionsAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException(
            "PredictionService (FR-2.x) is not implemented in this pass — see Section 5.3/FE-2.x.");

    public Task<IReadOnlyList<SuggestionSignal>> GetSignalsAsync(CancellationToken cancellationToken = default)
        => GetSuggestionsAsync(cancellationToken);
}
