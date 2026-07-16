using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.ViewModels;

/// <summary>Display-ready wrapper around a <see cref="Services.SuggestionSignal"/> for the Suggestions page.</summary>
public class SuggestionItem
{
    public required string ProductName { get; init; }
    public string? Category { get; init; }
    public required SuggestionUrgency Urgency { get; init; }
    public required string UrgencyText { get; init; }
    public required bool IsHighConfidence { get; init; }
    public required string ConfidenceText { get; init; }
    public required string IntervalText { get; init; }
    public required string LastPurchasedText { get; init; }
}
