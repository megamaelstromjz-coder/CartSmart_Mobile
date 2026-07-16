using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Data.Repositories;

/// <summary>Read-side of the cached reference list backing offline autocomplete (FE-1.4).</summary>
public interface IReferenceProductRepository
{
    Task<List<ReferenceProduct>> SearchAsync(string query);
    Task ReplaceAllAsync(IEnumerable<ReferenceProduct> products);
}
