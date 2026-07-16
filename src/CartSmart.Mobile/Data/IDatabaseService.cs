using SQLite;

namespace CartSmart.Mobile.Data;

/// <summary>
/// Owns the single SQLite connection and schema creation for all local tables (spec Section 4).
/// Repositories pull the connection from here rather than opening their own.
/// </summary>
public interface IDatabaseService
{
    Task<SQLiteAsyncConnection> GetConnectionAsync();
}
