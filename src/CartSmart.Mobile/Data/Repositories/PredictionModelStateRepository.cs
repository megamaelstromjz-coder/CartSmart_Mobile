using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Data.Repositories;

public class PredictionModelStateRepository(IDatabaseService db) : IPredictionModelStateRepository
{
    public async Task<List<PredictionModelState>> GetAllAsync()
    {
        var conn = await db.GetConnectionAsync();
        return await conn.Table<PredictionModelState>().ToListAsync();
    }

    public async Task<PredictionModelState?> GetByProductNameAsync(string productName)
    {
        var conn = await db.GetConnectionAsync();
        return await conn.Table<PredictionModelState>()
            .Where(s => s.ProductName == productName)
            .FirstOrDefaultAsync();
    }

    public async Task UpsertAsync(PredictionModelState state)
    {
        var conn = await db.GetConnectionAsync();
        await conn.InsertOrReplaceAsync(state);
    }
}
