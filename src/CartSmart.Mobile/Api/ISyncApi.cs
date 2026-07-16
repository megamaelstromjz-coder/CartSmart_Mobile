using CartSmart.Mobile.Api.Dtos;
using Refit;

namespace CartSmart.Mobile.Api;

/// <summary>Mirrors the `Sync` OpenAPI tag (spec Section 6.4, 1 endpoint) — the only pull mechanism.</summary>
public interface ISyncApi
{
    [Get("/api/v1/sync")]
    Task<SyncResponse> PullChangesAsync([Query] DateTimeOffset since);
}
