using System.Collections.ObjectModel;
using CartSmart.Mobile.Models;
using CartSmart.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartSmart.Mobile.ViewModels;

/// <summary>Purchase History: every tracked product's rolling prediction state (spec Section 5.3 supporting view).</summary>
public partial class HistoryViewModel(IPredictionService predictionService) : BaseViewModel
{
    private List<HistoryItem> allItems = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int trackedCount;

    [ObservableProperty]
    private int highConfidenceCount;

    [ObservableProperty]
    private int buildingDataCount;

    [ObservableProperty]
    private int coldStartCount;

    public ObservableCollection<HistoryItem> Items { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var states = await predictionService.GetHistoryAsync();
            allItems = states.Select(ToDisplayItem).ToList();

            TrackedCount = allItems.Count;
            HighConfidenceCount = allItems.Count(i => i.ConfidenceTier == "High confidence");
            BuildingDataCount = allItems.Count(i => i.ConfidenceTier == "Building data");
            ColdStartCount = allItems.Count(i => i.ConfidenceTier == "Cold start");

            ApplySearch();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplySearch();

    [RelayCommand]
    private static void ToggleExpand(HistoryItem item) => item.IsExpanded = !item.IsExpanded;

    private void ApplySearch()
    {
        Items.Clear();
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? allItems
            : allItems.Where(i => i.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var item in filtered)
        {
            Items.Add(item);
        }
    }

    private static HistoryItem ToDisplayItem(PredictionModelState state)
    {
        var tier = state.PurchaseCount >= PredictionService.HighConfidencePurchaseCount
            ? "High confidence"
            : state.PurchaseCount >= PredictionService.MinPurchasesForSuggestion
                ? "Building data"
                : "Cold start";

        var hasInterval = state.PurchaseCount >= PredictionService.MinPurchasesForSuggestion;
        var nextPredicted = state.LastPurchasedAt.AddDays(state.IntervalDays);

        return new HistoryItem
        {
            ProductName = state.ProductName,
            Category = state.Category,
            PurchaseCount = state.PurchaseCount,
            ConfidenceTier = tier,
            ConfidenceFraction = state.Weight,
            ConfidencePercentText = $"{(int)Math.Round(state.Weight * 100)}%",
            AvgIntervalText = hasInterval ? $"~{Math.Round(state.IntervalDays)} days" : "Not enough data",
            LastPurchasedText = state.LastPurchasedAt.LocalDateTime.ToString("yyyy-MM-dd"),
            NextPredictedText = hasInterval ? nextPredicted.LocalDateTime.ToString("yyyy-MM-dd") : "Not enough data",
        };
    }
}
