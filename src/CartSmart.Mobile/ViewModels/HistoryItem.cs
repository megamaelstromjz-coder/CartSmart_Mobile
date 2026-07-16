using CommunityToolkit.Mvvm.ComponentModel;

namespace CartSmart.Mobile.ViewModels;

/// <summary>Display-ready wrapper around a <see cref="Models.PredictionModelState"/> for the History page.</summary>
public partial class HistoryItem : ObservableObject
{
    public required string ProductName { get; init; }
    public string? Category { get; init; }
    public required int PurchaseCount { get; init; }
    public required string ConfidenceTier { get; init; }
    public required double ConfidenceFraction { get; init; }
    public required string ConfidencePercentText { get; init; }
    public required string AvgIntervalText { get; init; }
    public required string LastPurchasedText { get; init; }
    public required string NextPredictedText { get; init; }

    [ObservableProperty]
    private bool isExpanded;
}
