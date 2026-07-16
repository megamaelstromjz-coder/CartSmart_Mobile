using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Data.Repositories;

public interface IPredictionModelStateRepository
{
    Task<List<PredictionModelState>> GetAllAsync();
    Task<PredictionModelState?> GetByProductNameAsync(string productName);
    Task UpsertAsync(PredictionModelState state);
}
