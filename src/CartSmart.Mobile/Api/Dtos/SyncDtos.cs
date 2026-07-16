namespace CartSmart.Mobile.Api.Dtos;

/// <summary>
/// GET /sync response (Section 6.4). <c>ServerTimestamp</c> is the client's next `since`
/// cursor — persist and echo it back verbatim, never substitute the device clock.
/// </summary>
public record SyncResponse(
    DateTimeOffset ServerTimestamp,
    List<ListResponse> Lists,
    List<ListItemResponse> Items,
    List<string> DeletedListIds,
    List<string> DeletedItemIds);
