using CartSmart.Mobile.Data.Repositories;
using CartSmart.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartSmart.Mobile.ViewModels;

/// <summary>FE-1.2 item detail sheet: quantity, unit, category.</summary>
[QueryProperty(nameof(ItemId), "itemId")]
public partial class ItemDetailViewModel(
    IListService listService,
    IListItemRepository listItemRepository) : BaseViewModel
{
    [ObservableProperty]
    private string itemId = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private double quantity;

    [ObservableProperty]
    private string? unit;

    [ObservableProperty]
    private string? category;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var item = await listItemRepository.GetByIdAsync(ItemId);
        if (item is null)
        {
            return;
        }

        Name = item.Name;
        Quantity = item.Quantity;
        Unit = item.Unit;
        Category = item.Category;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var item = await listItemRepository.GetByIdAsync(ItemId);
        if (item is null)
        {
            return;
        }

        item.Name = Name;
        item.Quantity = Quantity;
        item.Unit = Unit;
        item.Category = Category;

        await listService.UpdateItemAsync(item);
        await Shell.Current.GoToAsync("..");
    }
}
