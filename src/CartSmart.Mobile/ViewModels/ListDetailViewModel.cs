using System.Collections.ObjectModel;
using CartSmart.Mobile.Data.Repositories;
using CartSmart.Mobile.Models;
using CartSmart.Mobile.Services;
using CartSmart.Mobile.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartSmart.Mobile.ViewModels;

/// <summary>FE-1.1 list view (add/edit/delete/check-off, category-grouped) and FE-1.4 offline autocomplete.</summary>
[QueryProperty(nameof(ListId), "listId")]
[QueryProperty(nameof(ListName), "listName")]
public partial class ListDetailViewModel(
    IListService listService,
    IReferenceProductRepository referenceProductRepository,
    IPredictionService predictionService) : BaseViewModel
{
    [ObservableProperty]
    private string listId = string.Empty;

    [ObservableProperty]
    private string listName = string.Empty;

    [ObservableProperty]
    private string newItemName = string.Empty;

    [ObservableProperty]
    private int remainingCount;

    [ObservableProperty]
    private int doneCount;

    [ObservableProperty]
    private bool isCheckedSectionExpanded = true;

    public ObservableCollection<ItemGroup> PendingGroups { get; } = [];
    public ObservableCollection<ListItem> CheckedItems { get; } = [];
    public ObservableCollection<ReferenceProduct> AutocompleteSuggestions { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(ListId))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var items = await listService.GetItemsAsync(ListId);

            PendingGroups.Clear();
            CheckedItems.Clear();

            var pendingByCategory = items
                .Where(i => !i.IsChecked)
                .GroupBy(i => string.IsNullOrEmpty(i.Category) ? "Other" : i.Category!);

            foreach (var group in pendingByCategory)
            {
                PendingGroups.Add(new ItemGroup(group.Key, group));
            }

            foreach (var item in items.Where(i => i.IsChecked))
            {
                CheckedItems.Add(item);
            }

            RemainingCount = items.Count(i => !i.IsChecked);
            DoneCount = items.Count(i => i.IsChecked);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchAutocompleteAsync()
    {
        AutocompleteSuggestions.Clear();
        foreach (var product in await referenceProductRepository.SearchAsync(NewItemName))
        {
            AutocompleteSuggestions.Add(product);
        }
    }

    [RelayCommand]
    private void PickSuggestion(ReferenceProduct product)
    {
        NewItemName = product.Name;
        AutocompleteSuggestions.Clear();
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NewItemName))
        {
            return;
        }

        var matched = AutocompleteSuggestions.FirstOrDefault(p =>
            p.Name.Equals(NewItemName, StringComparison.OrdinalIgnoreCase));

        await listService.AddItemAsync(
            ListId, NewItemName.Trim(), quantity: 1, unit: null, category: matched?.Category);

        NewItemName = string.Empty;
        AutocompleteSuggestions.Clear();
        await LoadAsync();
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
    private async Task DeleteItemAsync(ListItem item)
    {
        await listService.DeleteItemAsync(item.ItemId);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task UncheckAllAsync()
    {
        foreach (var item in CheckedItems.ToList())
        {
            await listService.SetItemCheckedAsync(item.ItemId, false);
        }

        await LoadAsync();
    }

    [RelayCommand]
    private void ToggleCheckedSection() => IsCheckedSectionExpanded = !IsCheckedSectionExpanded;

    [RelayCommand]
    private async Task OpenItemDetailAsync(ListItem item)
    {
        await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?itemId={item.ItemId}");
    }

    [RelayCommand]
    private async Task StartShoppingAsync()
    {
        await Shell.Current.GoToAsync(
            $"//ShopPage?listId={ListId}&listName={Uri.EscapeDataString(ListName)}");
    }
}
