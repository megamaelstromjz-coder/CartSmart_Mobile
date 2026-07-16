using System.Collections.ObjectModel;
using CartSmart.Mobile.Models;
using CartSmart.Mobile.Services;
using CartSmart.Mobile.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartSmart.Mobile.ViewModels;

/// <summary>FE-1.3 multi-list picker/tab UI.</summary>
public partial class ListsViewModel(IListService listService) : BaseViewModel
{
    public ObservableCollection<ShoppingList> Lists { get; } = [];

    [ObservableProperty]
    private string newListName = string.Empty;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Lists.Clear();
            foreach (var list in await listService.GetListsAsync())
            {
                Lists.Add(list);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateListAsync()
    {
        if (string.IsNullOrWhiteSpace(NewListName))
        {
            return;
        }

        var list = await listService.CreateListAsync(NewListName.Trim());
        Lists.Add(list);
        NewListName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteListAsync(ShoppingList list)
    {
        await listService.DeleteListAsync(list.ListId);
        Lists.Remove(list);
    }

    [RelayCommand]
    private static async Task OpenListAsync(ShoppingList list)
    {
        await Shell.Current.GoToAsync($"{nameof(ListDetailPage)}?listId={list.ListId}&listName={Uri.EscapeDataString(list.Name)}");
    }
}
