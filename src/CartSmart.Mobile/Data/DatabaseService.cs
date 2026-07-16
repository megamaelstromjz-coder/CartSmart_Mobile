using CartSmart.Mobile.Models;
using SQLite;

namespace CartSmart.Mobile.Data;

public class DatabaseService : IDatabaseService
{
    private const string DbFileName = "cartsmart.db3";
    private SQLiteAsyncConnection? _connection;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _initLock.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                return _connection;
            }

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, DbFileName);
            var connection = new SQLiteAsyncConnection(dbPath);

            // Full Section 4 schema. Only ShoppingList/ListItem have working repositories in this
            // pass (Section 5.2 vertical slice) — the rest are created now so later slices are
            // additive migrations, not a redesign (Section 10.1).
            await connection.CreateTableAsync<ShoppingList>();
            await connection.CreateTableAsync<ListItem>();
            await connection.CreateTableAsync<PurchaseEvent>();
            await connection.CreateTableAsync<PredictionModelState>();
            await connection.CreateTableAsync<DeviceRegistration>();
            await connection.CreateTableAsync<ReferenceProduct>();
            await connection.CreateTableAsync<AppSetting>();

            _connection = connection;
            return _connection;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
