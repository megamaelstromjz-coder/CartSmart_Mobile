using CartSmart.Mobile.Api;
using CartSmart.Mobile.Data.Repositories;
using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Services;

public class ListService(
    IListRepository listRepository,
    IListItemRepository listItemRepository,
    ISyncService syncService,
    IDeviceContext deviceContext) : IListService
{
    public Task<List<ShoppingList>> GetListsAsync() => listRepository.GetAllAsync();

    public async Task<ShoppingList> CreateListAsync(string name)
    {
        var list = new ShoppingList
        {
            Name = name,
            ClientDeviceId = await deviceContext.GetClientDeviceIdAsync(),
        };

        await listRepository.UpsertLocalAsync(list);
        await TryPushAsync();
        return list;
    }

    public async Task RenameListAsync(string listId, string newName)
    {
        var list = await listRepository.GetByIdAsync(listId);
        if (list is null)
        {
            return;
        }

        list.Name = newName;
        await listRepository.UpsertLocalAsync(list);
        await TryPushAsync();
    }

    public async Task DeleteListAsync(string listId)
    {
        await listRepository.DeleteLocalAsync(listId);
        await TryPushAsync();
    }

    public Task<List<ListItem>> GetItemsAsync(string listId) => listItemRepository.GetForListAsync(listId);

    public async Task<ListItem> AddItemAsync(string listId, string name, double quantity, string? unit, string? category)
    {
        var item = new ListItem
        {
            ListId = listId,
            Name = name,
            Quantity = quantity,
            Unit = unit,
            Category = category,
            ClientDeviceId = await deviceContext.GetClientDeviceIdAsync(),
        };

        await listItemRepository.UpsertLocalAsync(item);
        await TryPushAsync();
        return item;
    }

    public async Task UpdateItemAsync(ListItem item)
    {
        await listItemRepository.UpsertLocalAsync(item);
        await TryPushAsync();
    }

    public async Task SetItemCheckedAsync(string itemId, bool isChecked)
    {
        var item = await listItemRepository.GetByIdAsync(itemId);
        if (item is null)
        {
            return;
        }

        item.IsChecked = isChecked;
        await listItemRepository.UpsertLocalAsync(item);
        await TryPushAsync();
    }

    public async Task DeleteItemAsync(string itemId)
    {
        await listItemRepository.DeleteLocalAsync(itemId);
        await TryPushAsync();
    }

    /// <summary>
    /// Best-effort immediate push so the UI reflects sync state quickly when online; failures
    /// are swallowed here because the row is already marked dirty and will be retried by the
    /// next explicit or scheduled <see cref="ISyncService.PushPendingChangesAsync"/> call
    /// (FE-6.4: sync failures must never block list interaction).
    /// </summary>
    private async Task TryPushAsync()
    {
        try
        {
            await syncService.PushPendingChangesAsync();
        }
        catch (ApiException)
        {
        }
    }
}
