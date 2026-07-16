namespace CartSmart.Mobile.Api.Dtos;

public record UpsertListRequest(string Name);

public record ListResponse(string ListId, string Name, DateTimeOffset UpdatedAt);

public record UpsertListItemRequest(
    string Name,
    double Quantity,
    string? Unit,
    string? Category,
    bool IsChecked);

public record ListItemResponse(
    string ItemId,
    string ListId,
    string Name,
    double Quantity,
    string? Unit,
    string? Category,
    bool IsChecked,
    DateTimeOffset UpdatedAt);
