using CartSmart.Mobile.Api.Dtos;
using Refit;

namespace CartSmart.Mobile.Api;

/// <summary>Mirrors the `Reference` OpenAPI tag (spec Section 6.6, 2 endpoints).</summary>
public interface IReferenceApi
{
    [Get("/api/v1/reference/version")]
    Task<ReferenceVersionResponse> GetVersionAsync();

    [Get("/api/v1/reference/products")]
    Task<List<ReferenceProductResponse>> GetProductsAsync();
}
