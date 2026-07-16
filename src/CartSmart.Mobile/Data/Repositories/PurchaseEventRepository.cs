using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Data.Repositories;

public class PurchaseEventRepository(IDatabaseService db) : IPurchaseEventRepository
{
    public async Task InsertAsync(PurchaseEvent purchaseEvent)
    {
        var conn = await db.GetConnectionAsync();
        await conn.InsertAsync(purchaseEvent);
    }

    public async Task<List<PurchaseEvent>> GetForProductAsync(string productName)
    {
        var conn = await db.GetConnectionAsync();
        return await conn.Table<PurchaseEvent>()
            .Where(e => e.ProductName == productName)
            .OrderBy(e => e.PurchasedAt)
            .ToListAsync();
    }
}
