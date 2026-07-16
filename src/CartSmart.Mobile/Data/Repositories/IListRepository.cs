using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Data.Repositories;

public interface IListRepository
{
    Task<List<ShoppingList>> GetAllAsync();
    Task<ShoppingList?> GetByIdAsync(string listId);
    Task<List<ShoppingList>> GetDirtyAsync();
    Task UpsertLocalAsync(ShoppingList list);
    Task MarkSyncedAsync(string listId, DateTimeOffset serverUpdatedAt);
    Task DeleteLocalAsync(string listId);
    Task ApplyServerUpsertAsync(ShoppingList list);
    Task ApplyServerDeleteAsync(string listId);
}
