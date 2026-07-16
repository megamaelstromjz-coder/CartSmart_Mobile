using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Data.Repositories;

public interface IListItemRepository
{
    Task<List<ListItem>> GetForListAsync(string listId);
    Task<ListItem?> GetByIdAsync(string itemId);
    Task<List<ListItem>> GetDirtyAsync();
    Task UpsertLocalAsync(ListItem item);
    Task MarkSyncedAsync(string itemId, DateTimeOffset serverUpdatedAt);
    Task DeleteLocalAsync(string itemId);
    Task ApplyServerUpsertAsync(ListItem item);
    Task ApplyServerDeleteAsync(string itemId);
}
