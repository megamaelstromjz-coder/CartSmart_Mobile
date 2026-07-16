using CartSmart.Mobile.Api.Dtos;
using Refit;

namespace CartSmart.Mobile.Api;

/// <summary>Mirrors the `Account` OpenAPI tag (spec Section 6.5, 3 endpoints).</summary>
public interface IAccountApi
{
    [Get("/api/v1/account")]
    Task<AccountResponse> GetAccountAsync();

    [Delete("/api/v1/account")]
    Task DeleteAccountAsync();

    [Get("/api/v1/account/export")]
    Task<AccountExport> ExportAccountAsync();
}
