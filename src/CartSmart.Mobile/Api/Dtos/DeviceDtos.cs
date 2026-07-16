namespace CartSmart.Mobile.Api.Dtos;

public record RegisterDeviceRequest(string ClientDeviceId, string Platform, string DisplayName);

public record DeviceResponse(
    string DeviceId,
    string ClientDeviceId,
    string Platform,
    string DisplayName,
    DateTimeOffset RegisteredAt);

public record DeviceListEntry(
    string DeviceId,
    string ClientDeviceId,
    string Platform,
    string DisplayName,
    DateTimeOffset? LastSyncAt);
