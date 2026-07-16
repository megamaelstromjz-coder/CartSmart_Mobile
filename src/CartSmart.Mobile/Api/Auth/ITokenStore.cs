namespace CartSmart.Mobile.Api.Auth;

/// <summary>
/// Auth token persistence via SecureStorage (spec Section 2) — never purchase data, only tokens.
/// </summary>
public interface ITokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SaveAsync(string accessToken, string refreshToken);
    Task ClearAsync();
}
