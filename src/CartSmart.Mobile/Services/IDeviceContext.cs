namespace CartSmart.Mobile.Services;

/// <summary>
/// This device's stable identity for attribution (Section 4 `client_device_id`,
/// Section 6.2 `clientDeviceId`). Per spec: locally-generated GUID persisted in SecureStorage,
/// not the OS device identifier, which can change.
/// </summary>
public interface IDeviceContext
{
    Task<string> GetClientDeviceIdAsync();
    string Platform { get; }
}

public class DeviceContext : IDeviceContext
{
    private const string ClientDeviceIdKey = "cartsmart_client_device_id";
    private string? _cached;

    public string Platform => DeviceInfo.Platform == DevicePlatform.iOS ? "iOS" : "Android";

    public async Task<string> GetClientDeviceIdAsync()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var existing = await SecureStorage.Default.GetAsync(ClientDeviceIdKey);
        if (!string.IsNullOrEmpty(existing))
        {
            _cached = existing;
            return _cached;
        }

        var generated = Guid.NewGuid().ToString();
        await SecureStorage.Default.SetAsync(ClientDeviceIdKey, generated);
        _cached = generated;
        return _cached;
    }
}
