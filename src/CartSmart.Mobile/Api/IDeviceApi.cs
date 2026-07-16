using CartSmart.Mobile.Api.Dtos;
using Refit;

namespace CartSmart.Mobile.Api;

/// <summary>Mirrors the `Device` OpenAPI tag (spec Section 6.2, 3 endpoints).</summary>
public interface IDeviceApi
{
    [Post("/api/v1/devices")]
    Task<DeviceResponse> RegisterDeviceAsync([Body] RegisterDeviceRequest request);

    [Get("/api/v1/devices")]
    Task<List<DeviceListEntry>> GetDevicesAsync();

    [Delete("/api/v1/devices/{deviceId}")]
    Task RemoveDeviceAsync(string deviceId);
}
