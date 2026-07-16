using System.Collections.ObjectModel;
using CartSmart.Mobile.Models;
using CartSmart.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartSmart.Mobile.ViewModels;

/// <summary>FE-2.x Smart Suggestions: overdue/due-soon/upcoming items ranked by <see cref="ISuggestionSignalProvider"/>.</summary>
public partial class SuggestionsViewModel(
    ISuggestionSignalProvider suggestionSignalProvider,
    IPredictionService predictionService,
    IListService listService) : BaseViewModel
{
    private List<SuggestionItem> allSuggestions = [];

    [ObservableProperty]
    private string selectedFilter = "All";

    [ObservableProperty]
    private int overdueCount;

    [ObservableProperty]
    private int dueSoonCount;

    [ObservableProperty]
    private int upcomingCount;

    public int TotalCount => OverdueCount + DueSoonCount + UpcomingCount;

    public ObservableCollection<SuggestionItem> Suggestions { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var signals = await suggestionSignalProvider.GetSignalsAsync();
            var now = DateTimeOffset.UtcNow;

            allSuggestions = signals.Select(s => ToDisplayItem(s, now)).ToList();

            OverdueCount = allSuggestions.Count(s => s.Urgency == SuggestionUrgency.Overdue);
            DueSoonCount = allSuggestions.Count(s => s.Urgency == SuggestionUrgency.DueSoon);
            UpcomingCount = allSuggestions.Count(s => s.Urgency == SuggestionUrgency.Upcoming);
            OnPropertyChanged(nameof(TotalCount));

            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SetFilter(string filter) => SelectedFilter = filter;

    partial void OnSelectedFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Suggestions.Clear();
        var filtered = SelectedFilter switch
        {
            "Overdue" => allSuggestions.Where(s => s.Urgency == SuggestionUrgency.Overdue),
            "Due soon" => allSuggestions.Where(s => s.Urgency == SuggestionUrgency.DueSoon),
            "Upcoming" => allSuggestions.Where(s => s.Urgency == SuggestionUrgency.Upcoming),
            _ => allSuggestions,
        };

        foreach (var item in filtered)
        {
            Suggestions.Add(item);
        }
    }

    [RelayCommand]
    private async Task SnoozeAsync(SuggestionItem item)
    {
        await predictionService.SnoozeAsync(item.ProductName, TimeSpan.FromDays(3));
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DismissAsync(SuggestionItem item)
    {
        await predictionService.DismissAsync(item.ProductName);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddToListAsync(SuggestionItem item)
    {
        var lists = await listService.GetListsAsync();
        var targetList = lists.FirstOrDefault();
        if (targetList is null)
        {
            return;
        }

        await listService.AddItemAsync(targetList.ListId, item.ProductName, quantity: 1, unit: null, item.Category);
    }

    private static SuggestionItem ToDisplayItem(SuggestionSignal signal, DateTimeOffset now)
    {
        var daysUntilNeeded = (signal.PredictedNeedBy - now).TotalDays;
        var urgency = daysUntilNeeded switch
        {
            < 0 => SuggestionUrgency.Overdue,
            <= 2 => SuggestionUrgency.DueSoon,
            _ => SuggestionUrgency.Upcoming,
        };

        var urgencyText = urgency switch
        {
            SuggestionUrgency.Overdue => $"{(int)Math.Ceiling(-daysUntilNeeded)} days overdue",
            SuggestionUrgency.DueSoon when daysUntilNeeded < 1 => "Due today",
            SuggestionUrgency.DueSoon => $"Due in {(int)Math.Ceiling(daysUntilNeeded)} days",
            _ => $"In {(int)Math.Ceiling(daysUntilNeeded)} days",
        };

        var isHighConfidence = signal.Confidence >= 0.99;

        return new SuggestionItem
        {
            ProductName = signal.ProductName,
            Category = signal.Category,
            Urgency = urgency,
            UrgencyText = urgencyText,
            IsHighConfidence = isHighConfidence,
            ConfidenceText = isHighConfidence ? "High confidence" : "Low confidence",
            IntervalText = $"Every ~{Math.Round(signal.AvgIntervalDays)} days",
            LastPurchasedText = $"Last bought {signal.LastPurchasedAt.LocalDateTime:yyyy-MM-dd}",
        };
    }
}
