using System.Collections.ObjectModel;
using CartSmart.Mobile.Data.Repositories;
using CartSmart.Mobile.Models;
using CartSmart.Mobile.Services;
using CartSmart.Mobile.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartSmart.Mobile.ViewModels;

/// <summary>FE-1.1 list view (add/edit/delete/swipe-check-off) and FE-1.4 offline autocomplete.</summary>
[QueryProperty(nameof(ListId), "listId")]
[QueryProperty(nameof(ListName), "listName")]
public partial class ListDetailViewModel(
    IListService listService,
    IReferenceProductRepository referenceProductRepository) : BaseViewModel
{
    [ObservableProperty]
    private string listId = string.Empty;

    [ObservableProperty]
    private string listName = string.Empty;

    [ObservableProperty]
    private string newItemName = string.Empty;

    public ObservableCollection<ListItem> Items { get; } = [];
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
            Items.Clear();
            foreach (var item in await listService.GetItemsAsync(ListId))
            {
                Items.Add(item);
            }
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

        var item = await listService.AddItemAsync(
            ListId, NewItemName.Trim(), quantity: 1, unit: null, category: matched?.Category);

        Items.Add(item);
        NewItemName = string.Empty;
        AutocompleteSuggestions.Clear();
    }

    [RelayCommand]
    private async Task ToggleCheckedAsync(ListItem item)
    {
        item.IsChecked = !item.IsChecked;
        await listService.SetItemCheckedAsync(item.ItemId, item.IsChecked);
    }

    [RelayCommand]
    private async Task DeleteItemAsync(ListItem item)
    {
        await listService.DeleteItemAsync(item.ItemId);
        Items.Remove(item);
    }

    [RelayCommand]
    private async Task OpenItemDetailAsync(ListItem item)
    {
        await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?itemId={item.ItemId}");
    }
}
