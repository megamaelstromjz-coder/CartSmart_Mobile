using System.Net;
using System.Net.Http.Headers;
using CartSmart.Mobile.Api.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace CartSmart.Mobile.Api.Auth;

/// <summary>
/// Attaches the bearer access token to every request and transparently refreshes on a 401
/// (spec Section 6.1: "called transparently ... on 401"). This is the one place token
/// attachment/refresh happens — <c>SyncService</c> never touches tokens directly.
/// </summary>
public class AuthTokenDelegatingHandler(ITokenStore tokenStore, IServiceProvider serviceProvider) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var requestForRetry = await CloneAsync(request);
        var response = await base.SendAsync(request, cancellationToken);

        var isRefreshEndpoint = request.RequestUri?.AbsolutePath.EndsWith("/auth/refresh", StringComparison.Ordinal) == true;
        if (response.StatusCode != HttpStatusCode.Unauthorized || isRefreshEndpoint)
        {
            return response;
        }

        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            var refreshToken = await tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                return response;
            }

            var authApi = serviceProvider.GetRequiredService<IAuthApi>();
            var refreshed = await authApi.RefreshAsync(new RefreshRequest(refreshToken));
            await tokenStore.SaveAsync(refreshed.AccessToken, refreshed.RefreshToken);

            requestForRetry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
            response.Dispose();
            return await base.SendAsync(requestForRetry, cancellationToken);
        }
        catch (Refit.ApiException)
        {
            // Refresh token itself is invalid/expired — force a real re-login rather than looping.
            await tokenStore.ClearAsync();
            return response;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.Add(header.Key, header.Value);
            }
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
