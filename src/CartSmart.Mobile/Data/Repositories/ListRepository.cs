using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Data.Repositories;

public class ListRepository(IDatabaseService db) : IListRepository
{
    public async Task<List<ShoppingList>> GetAllAsync()
    {
        var conn = await db.GetConnectionAsync();
        return await conn.Table<ShoppingList>()
            .Where(l => !l.IsDeleted)
            .ToListAsync();
    }

    public async Task<ShoppingList?> GetByIdAsync(string listId)
    {
        var conn = await db.GetConnectionAsync();
        return await conn.Table<ShoppingList>()
            .Where(l => l.ListId == listId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ShoppingList>> GetDirtyAsync()
    {
        var conn = await db.GetConnectionAsync();
        return await conn.Table<ShoppingList>()
            .Where(l => l.IsDirty)
            .ToListAsync();
    }

    public async Task UpsertLocalAsync(ShoppingList list)
    {
        var conn = await db.GetConnectionAsync();
        list.IsDirty = true;
        await conn.InsertOrReplaceAsync(list);
    }

    public async Task MarkSyncedAsync(string listId, DateTimeOffset serverUpdatedAt)
    {
        var conn = await db.GetConnectionAsync();
        var list = await GetByIdAsync(listId);
        if (list is null)
        {
            return;
        }

        list.IsDirty = false;
        list.ServerUpdatedAt = serverUpdatedAt;
        await conn.UpdateAsync(list);
    }

    public async Task DeleteLocalAsync(string listId)
    {
        var conn = await db.GetConnectionAsync();
        var list = await GetByIdAsync(listId);
        if (list is null)
        {
            return;
        }

        // Soft-delete and mark dirty so the offline queue (Section 7) can replay a DELETE
        // against the API once connectivity returns; a hard delete would lose that intent.
        list.IsDeleted = true;
        list.IsDirty = true;
        await conn.UpdateAsync(list);
    }

    public async Task ApplyServerUpsertAsync(ShoppingList list)
    {
        var conn = await db.GetConnectionAsync();
        var existing = await GetByIdAsync(list.ListId);

        // FE-6.3: don't clobber a not-yet-pushed local edit with an incoming pull.
        if (existing is { IsDirty: true })
        {
            return;
        }

        list.IsDirty = false;
        await conn.InsertOrReplaceAsync(list);
    }

    public async Task ApplyServerDeleteAsync(string listId)
    {
        var conn = await db.GetConnectionAsync();
        var existing = await GetByIdAsync(listId);
        if (existing is null)
        {
            return;
        }

        await conn.DeleteAsync(existing);
    }
}
