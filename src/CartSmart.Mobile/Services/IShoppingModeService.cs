using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Services;

/// <summary>
/// FR-4.x Shopping Mode (spec Section 5.5) — large-tap checklist, category/aisle sort, fully
/// offline. Must never call <c>SyncService</c> (FE-4.3, NFR-3).
/// </summary>
public interface IShoppingModeService
{
    Task<List<ListItem>> GetSortedForShoppingAsync(string listId);
}
