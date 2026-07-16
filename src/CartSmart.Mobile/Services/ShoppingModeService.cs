using CartSmart.Mobile.Data.Repositories;
using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Services;

public class ShoppingModeService(IListItemRepository listItemRepository) : IShoppingModeService
{
    public async Task<List<ListItem>> GetSortedForShoppingAsync(string listId)
    {
        var items = await listItemRepository.GetForListAsync(listId);
        return items
            .OrderBy(i => i.Category ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
