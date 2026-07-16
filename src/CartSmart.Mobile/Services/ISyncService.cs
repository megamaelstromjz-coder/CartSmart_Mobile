using CartSmart.Mobile.Api.Dtos;

namespace CartSmart.Mobile.Services;

/// <summary>
/// The only component allowed to call the backend API (spec Section 3). Owns auth-token
/// attachment (delegated to <c>AuthTokenDelegatingHandler</c>), retry/backoff for the offline
/// queue, and pull-sync. Never touches purchase history or model state.
///
/// Auth/Device/List/Sync/Reference are fully implemented — the minimum needed to make the
/// Section 5.2 List Management vertical slice work end-to-end. Account is not (no screen in
/// this pass depends on it); see <see cref="GetAccountAsync"/>/<see cref="DeleteAccountAsync"/>/
/// <see cref="ExportAccountDataAsync"/>.
/// </summary>
public interface ISyncService
{
    // Auth (6.1) — needed to obtain the tokens AuthTokenDelegatingHandler attaches.
    Task<AuthSession> RegisterAsync(string email, string password);
    Task<AuthSession> LoginAsync(string email, string password);
    Task LogoutAsync();

    // Device (6.2) — FE-6.5: must run once after login, before any list sync.
    Task EnsureDeviceRegisteredAsync();

    // List/Item (6.3) + offline queue (Section 7).
    Task<ListResponse> UpsertListAsync(ShoppingListPush list);
    Task DeleteListAsync(string listId);
    Task<ListItemResponse> UpsertItemAsync(ListItemPush item);
    Task DeleteItemAsync(string listId, string itemId);

    /// <summary>Replays every dirty local list/item as an individual PUT/DELETE, in order (FE-6.2).</summary>
    Task PushPendingChangesAsync();

    /// <summary>Pulls since the last persisted cursor and applies additions/updates/deletes locally (Section 6.4).</summary>
    Task<SyncResult> PullChangesAsync();

    // Reference (6.6) — backs FE-1.4 offline autocomplete.
    Task RefreshReferenceDataIfNeededAsync();

    // Account (6.5) — not wired to a screen in this pass.
    Task<AccountResponse> GetAccountAsync();
    Task DeleteAccountAsync();
    Task<AccountExport> ExportAccountDataAsync();
}

public record ShoppingListPush(string ListId, string Name);
public record ListItemPush(string ItemId, string ListId, string Name, double Quantity, string? Unit, string? Category, bool IsChecked);

/// <summary>
/// Lists/items whose local edit was superseded by a newer server write discovered during pull
/// (FE-6.3 "updated on another device" detection).
/// </summary>
public record SyncResult(IReadOnlyList<string> ConflictedListIds, IReadOnlyList<string> ConflictedItemIds);
