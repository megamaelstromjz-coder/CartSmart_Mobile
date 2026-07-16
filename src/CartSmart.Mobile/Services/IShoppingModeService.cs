using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Services;

/// <summary>
/// FR-4.x Shopping Mode (spec Section 5.5) — large-tap checklist, category/aisle sort, fully
/// offline. Out of scope for this pass; stubbed so DI wiring is in place. Note it must never
/// call <c>SyncService</c> (FE-4.3, NFR-3) once implemented.
/// </summary>
public interface IShoppingModeService
{
    Task<List<ListItem>> GetSortedForShoppingAsync(string listId);
}

public class ShoppingModeService : IShoppingModeService
{
    public Task<List<ListItem>> GetSortedForShoppingAsync(string listId)
        => throw new NotImplementedException(
            "ShoppingModeService (FR-4.x) is not implemented in this pass — see Section 5.5.");
}
