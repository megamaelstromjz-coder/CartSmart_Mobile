using CartSmart.Mobile.Api.Dtos;
using Refit;

namespace CartSmart.Mobile.Api;

/// <summary>Mirrors the `Auth` OpenAPI tag exactly (spec Section 6.1, 9 endpoints).</summary>
public interface IAuthApi
{
    [Post("/api/v1/auth/register")]
    Task<AuthSession> RegisterAsync([Body] RegisterRequest request);

    [Post("/api/v1/auth/login")]
    Task<AuthSession> LoginAsync([Body] LoginRequest request);

    [Post("/api/v1/auth/google")]
    Task<AuthSession> LoginWithGoogleAsync([Body] ExternalLoginRequest request);

    [Post("/api/v1/auth/apple")]
    Task<AuthSession> LoginWithAppleAsync([Body] ExternalLoginRequest request);

    [Post("/api/v1/auth/refresh")]
    Task<RefreshedSession> RefreshAsync([Body] RefreshRequest request);

    [Post("/api/v1/auth/logout")]
    Task LogoutAsync([Body] LogoutRequest request);

    [Post("/api/v1/auth/password/forgot")]
    Task RequestPasswordResetAsync([Body] ForgotPasswordRequest request);

    [Post("/api/v1/auth/password/reset")]
    Task CompletePasswordResetAsync([Body] ResetPasswordRequest request);

    [Post("/api/v1/auth/password/change")]
    Task ChangePasswordAsync([Body] ChangePasswordRequest request);
}
