using CartSmart.Mobile.Api.Dtos;
using Refit;

namespace CartSmart.Mobile.Api;

/// <summary>
/// Mirrors the `List` OpenAPI tag (spec Section 6.3, 4 endpoints). No bulk push endpoint exists —
/// the offline queue (Section 7) must replay these one at a time, in order.
/// </summary>
public interface IListApi
{
    [Put("/api/v1/lists/{listId}")]
    Task<ListResponse> UpsertListAsync(string listId, [Body] UpsertListRequest request);

    [Delete("/api/v1/lists/{listId}")]
    Task DeleteListAsync(string listId);

    [Put("/api/v1/lists/{listId}/items/{itemId}")]
    Task<ListItemResponse> UpsertItemAsync(string listId, string itemId, [Body] UpsertListItemRequest request);

    [Delete("/api/v1/lists/{listId}/items/{itemId}")]
    Task DeleteItemAsync(string listId, string itemId);
}
