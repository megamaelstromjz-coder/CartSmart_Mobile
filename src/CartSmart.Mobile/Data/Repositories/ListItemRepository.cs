using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Data.Repositories;

public class ListItemRepository(IDatabaseService db) : IListItemRepository
{
    public async Task<List<ListItem>> GetForListAsync(string listId)
    {
        var conn = await db.GetConnectionAsync();
        return await conn.Table<ListItem>()
            .Where(i => i.ListId == listId && !i.IsDeleted)
            .ToListAsync();
    }

    public async Task<ListItem?> GetByIdAsync(string itemId)
    {
        var conn = await db.GetConnectionAsync();
        return await conn.Table<ListItem>()
            .Where(i => i.ItemId == itemId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ListItem>> GetDirtyAsync()
    {
        var conn = await db.GetConnectionAsync();
        return await conn.Table<ListItem>()
            .Where(i => i.IsDirty)
            .ToListAsync();
    }

    public async Task UpsertLocalAsync(ListItem item)
    {
        var conn = await db.GetConnectionAsync();
        item.IsDirty = true;
        await conn.InsertOrReplaceAsync(item);
    }

    public async Task MarkSyncedAsync(string itemId, DateTimeOffset serverUpdatedAt)
    {
        var conn = await db.GetConnectionAsync();
        var item = await GetByIdAsync(itemId);
        if (item is null)
        {
            return;
        }

        item.IsDirty = false;
        item.ServerUpdatedAt = serverUpdatedAt;
        await conn.UpdateAsync(item);
    }

    public async Task DeleteLocalAsync(string itemId)
    {
        var conn = await db.GetConnectionAsync();
        var item = await GetByIdAsync(itemId);
        if (item is null)
        {
            return;
        }

        item.IsDeleted = true;
        item.IsDirty = true;
        await conn.UpdateAsync(item);
    }

    public async Task ApplyServerUpsertAsync(ListItem item)
    {
        var conn = await db.GetConnectionAsync();
        var existing = await GetByIdAsync(item.ItemId);

        if (existing is { IsDirty: true })
        {
            return;
        }

        item.IsDirty = false;
        await conn.InsertOrReplaceAsync(item);
    }

    public async Task ApplyServerDeleteAsync(string itemId)
    {
        var conn = await db.GetConnectionAsync();
        var existing = await GetByIdAsync(itemId);
        if (existing is null)
        {
            return;
        }

        await conn.DeleteAsync(existing);
    }
}
