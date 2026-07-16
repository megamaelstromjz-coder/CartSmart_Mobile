using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Services;

/// <summary>
/// FR-1.x list/item management (spec Section 3). Owns local SQLite reads/writes; all backend
/// interaction is delegated to <see cref="ISyncService"/> — this class never touches an I*Api
/// interface directly.
/// </summary>
public interface IListService
{
    Task<List<ShoppingList>> GetListsAsync();
    Task<ShoppingList> CreateListAsync(string name);
    Task RenameListAsync(string listId, string newName);
    Task DeleteListAsync(string listId);

    Task<List<ListItem>> GetItemsAsync(string listId);
    Task<ListItem> AddItemAsync(string listId, string name, double quantity, string? unit, string? category);
    Task UpdateItemAsync(ListItem item);
    Task SetItemCheckedAsync(string itemId, bool isChecked);
    Task DeleteItemAsync(string itemId);
}
