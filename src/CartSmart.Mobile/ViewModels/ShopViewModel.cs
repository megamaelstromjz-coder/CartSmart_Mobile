using System.Collections.ObjectModel;
using CartSmart.Mobile.Models;
using CartSmart.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartSmart.Mobile.ViewModels;

/// <summary>FE-4.x Shopping Mode: large-tap, category-grouped checklist for a single list.</summary>
[QueryProperty(nameof(ListId), "listId")]
[QueryProperty(nameof(ListName), "listName")]
public partial class ShopViewModel(
    IShoppingModeService shoppingModeService,
    IListService listService,
    IPredictionService predictionService) : BaseViewModel
{
    [ObservableProperty]
    private string listId = string.Empty;

    [ObservableProperty]
    private string listName = string.Empty;

    [ObservableProperty]
    private int checkedCount;

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private double progress;

    public string ProgressText => TotalCount == 0 ? "0%" : $"{(int)Math.Round(Progress * 100)}%";

    public ObservableCollection<ItemGroup> PendingGroups { get; } = [];
    public ObservableCollection<ListItem> BasketItems { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            if (string.IsNullOrEmpty(ListId))
            {
                var firstList = (await listService.GetListsAsync()).FirstOrDefault();
                if (firstList is null)
                {
                    return;
                }

                ListId = firstList.ListId;
                ListName = firstList.Name;
            }

            var items = await shoppingModeService.GetSortedForShoppingAsync(ListId);

            PendingGroups.Clear();
            BasketItems.Clear();

            var pendingByCategory = items
                .Where(i => !i.IsChecked)
                .GroupBy(i => string.IsNullOrEmpty(i.Category) ? "Other" : i.Category!);

            foreach (var group in pendingByCategory)
            {
                PendingGroups.Add(new ItemGroup(group.Key, group));
            }

            foreach (var item in items.Where(i => i.IsChecked))
            {
                BasketItems.Add(item);
            }

            TotalCount = items.Count;
            CheckedCount = items.Count(i => i.IsChecked);
            Progress = TotalCount == 0 ? 0 : (double)CheckedCount / TotalCount;
            OnPropertyChanged(nameof(ProgressText));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleCheckedAsync(ListItem item)
    {
        var newValue = !item.IsChecked;
        await listService.SetItemCheckedAsync(item.ItemId, newValue);

        if (newValue)
        {
            await predictionService.RecordPurchaseAsync(item.Name, item.Quantity, item.Unit, item.Category);
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        foreach (var item in BasketItems.ToList())
        {
            await listService.SetItemCheckedAsync(item.ItemId, false);
        }

        await LoadAsync();
    }
}
