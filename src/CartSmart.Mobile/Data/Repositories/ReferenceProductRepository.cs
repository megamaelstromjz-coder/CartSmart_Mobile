using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Data.Repositories;

public class ReferenceProductRepository(IDatabaseService db) : IReferenceProductRepository
{
    public async Task<List<ReferenceProduct>> SearchAsync(string query)
    {
        var conn = await db.GetConnectionAsync();
        if (string.IsNullOrWhiteSpace(query))
        {
            return await conn.Table<ReferenceProduct>().Take(20).ToListAsync();
        }

        var all = await conn.Table<ReferenceProduct>().ToListAsync();
        return all
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToList();
    }

    public async Task ReplaceAllAsync(IEnumerable<ReferenceProduct> products)
    {
        var conn = await db.GetConnectionAsync();
        await conn.RunInTransactionAsync(transactionConn =>
        {
            transactionConn.DeleteAll<ReferenceProduct>();
            transactionConn.InsertAll(products);
        });
    }
}
