using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Data.Repositories;

public interface IPurchaseEventRepository
{
    Task InsertAsync(PurchaseEvent purchaseEvent);
    Task<List<PurchaseEvent>> GetForProductAsync(string productName);
}
