using CartSmart.Mobile.Api;
using CartSmart.Mobile.Api.Auth;
using CartSmart.Mobile.Api.Dtos;
using CartSmart.Mobile.Data;
using CartSmart.Mobile.Data.Repositories;
using CartSmart.Mobile.Models;
using RefitApiException = Refit.ApiException;

namespace CartSmart.Mobile.Services;

public class SyncService(
    IAuthApi authApi,
    IDeviceApi deviceApi,
    IListApi listApi,
    ISyncApi syncApi,
    IAccountApi accountApi,
    IReferenceApi referenceApi,
    ITokenStore tokenStore,
    IDeviceContext deviceContext,
    IListRepository listRepository,
    IListItemRepository listItemRepository,
    IReferenceProductRepository referenceProductRepository,
    IDatabaseService database) : ISyncService
{
    public async Task<AuthSession> RegisterAsync(string email, string password)
    {
        var session = await CallAsync(() => authApi.RegisterAsync(new RegisterRequest(email, password)));
        await tokenStore.SaveAsync(session.AccessToken, session.RefreshToken);
        return session;
    }

    public async Task<AuthSession> LoginAsync(string email, string password)
    {
        var session = await CallAsync(() => authApi.LoginAsync(new LoginRequest(email, password)));
        await tokenStore.SaveAsync(session.AccessToken, session.RefreshToken);
        return session;
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await tokenStore.GetRefreshTokenAsync();
        if (!string.IsNullOrEmpty(refreshToken))
        {
            try
            {
                await CallAsync(() => authApi.LogoutAsync(new LogoutRequest(refreshToken)));
            }
            catch (ApiException)
            {
                // Best-effort server-side revoke — clearing local tokens below is what actually matters.
            }
        }

        await tokenStore.ClearAsync();
    }

    public async Task EnsureDeviceRegisteredAsync()
    {
        var conn = await database.GetConnectionAsync();
        var clientDeviceId = await deviceContext.GetClientDeviceIdAsync();
        var existing = await conn.Table<DeviceRegistration>().FirstOrDefaultAsync();

        if (existing?.DeviceId is not null)
        {
            return;
        }

        var response = await CallAsync(() => deviceApi.RegisterDeviceAsync(
            new RegisterDeviceRequest(clientDeviceId, deviceContext.Platform, deviceContext.Platform + " device")));

        await conn.InsertOrReplaceAsync(new DeviceRegistration
        {
            ClientDeviceId = clientDeviceId,
            DeviceId = response.DeviceId,
            Platform = deviceContext.Platform,
            DisplayName = response.DisplayName,
            RegisteredAt = response.RegisteredAt,
        });
    }

    public async Task<ListResponse> UpsertListAsync(ShoppingListPush list)
    {
        var response = await CallAsync(() => listApi.UpsertListAsync(list.ListId, new UpsertListRequest(list.Name)));
        await listRepository.MarkSyncedAsync(list.ListId, response.UpdatedAt);
        return response;
    }

    public async Task DeleteListAsync(string listId)
    {
        await CallAsync(() => listApi.DeleteListAsync(listId));
    }

    public async Task<ListItemResponse> UpsertItemAsync(ListItemPush item)
    {
        var response = await CallAsync(() => listApi.UpsertItemAsync(
            item.ListId, item.ItemId,
            new UpsertListItemRequest(item.Name, item.Quantity, item.Unit, item.Category, item.IsChecked)));
        await listItemRepository.MarkSyncedAsync(item.ItemId, response.UpdatedAt);
        return response;
    }

    public async Task DeleteItemAsync(string listId, string itemId)
    {
        await CallAsync(() => listApi.DeleteItemAsync(listId, itemId));
    }

    public async Task PushPendingChangesAsync()
    {
        // Order matters: a list must exist server-side before its items are pushed.
        foreach (var list in await listRepository.GetDirtyAsync())
        {
            await PushOneAsync(async () =>
            {
                if (list.IsDeleted)
                {
                    await DeleteListAsync(list.ListId);
                    await ClearLocalRowAsync(list.ListId, isList: true);
                }
                else
                {
                    await UpsertListAsync(new ShoppingListPush(list.ListId, list.Name));
                }
            });
        }

        foreach (var item in await listItemRepository.GetDirtyAsync())
        {
            await PushOneAsync(async () =>
            {
                if (item.IsDeleted)
                {
                    await DeleteItemAsync(item.ListId, item.ItemId);
                    await ClearLocalRowAsync(item.ItemId, isList: false);
                }
                else
                {
                    await UpsertItemAsync(new ListItemPush(
                        item.ItemId, item.ListId, item.Name, item.Quantity, item.Unit, item.Category, item.IsChecked));
                }
            });
        }
    }

    private static async Task PushOneAsync(Func<Task> push)
    {
        try
        {
            await push();
        }
        catch (ApiException)
        {
            // Leave the row dirty — it'll be retried on the next PushPendingChangesAsync call
            // (Section 7/FE-6.2: sync failures must never block list interaction).
        }
    }

    private async Task ClearLocalRowAsync(string id, bool isList)
    {
        var conn = await database.GetConnectionAsync();
        if (isList)
        {
            var row = await listRepository.GetByIdAsync(id);
            if (row is not null)
            {
                await conn.DeleteAsync(row);
            }
        }
        else
        {
            var row = await listItemRepository.GetByIdAsync(id);
            if (row is not null)
            {
                await conn.DeleteAsync(row);
            }
        }
    }

    public async Task<SyncResult> PullChangesAsync()
    {
        var conn = await database.GetConnectionAsync();
        var cursorSetting = await conn.Table<AppSetting>()
            .Where(s => s.Key == AppSetting.Keys.LastSyncCursor)
            .FirstOrDefaultAsync();
        var since = cursorSetting is not null
            ? DateTimeOffset.Parse(cursorSetting.Value)
            : DateTimeOffset.MinValue;

        var response = await CallAsync(() => syncApi.PullChangesAsync(since));

        var conflictedListIds = new List<string>();
        var conflictedItemIds = new List<string>();

        foreach (var listDto in response.Lists)
        {
            var existing = await listRepository.GetByIdAsync(listDto.ListId);
            if (existing is { IsDirty: true } && listDto.UpdatedAt > existing.ServerUpdatedAt)
            {
                conflictedListIds.Add(listDto.ListId);
                continue;
            }

            await listRepository.ApplyServerUpsertAsync(new ShoppingList
            {
                ListId = listDto.ListId,
                Name = listDto.Name,
                ServerUpdatedAt = listDto.UpdatedAt,
            });
        }

        foreach (var itemDto in response.Items)
        {
            var existing = await listItemRepository.GetByIdAsync(itemDto.ItemId);
            if (existing is { IsDirty: true } && itemDto.UpdatedAt > existing.ServerUpdatedAt)
            {
                conflictedItemIds.Add(itemDto.ItemId);
                continue;
            }

            await listItemRepository.ApplyServerUpsertAsync(new ListItem
            {
                ItemId = itemDto.ItemId,
                ListId = itemDto.ListId,
                Name = itemDto.Name,
                Quantity = itemDto.Quantity,
                Unit = itemDto.Unit,
                Category = itemDto.Category,
                IsChecked = itemDto.IsChecked,
                ServerUpdatedAt = itemDto.UpdatedAt,
            });
        }

        foreach (var deletedListId in response.DeletedListIds)
        {
            await listRepository.ApplyServerDeleteAsync(deletedListId);
        }

        foreach (var deletedItemId in response.DeletedItemIds)
        {
            await listItemRepository.ApplyServerDeleteAsync(deletedItemId);
        }

        // Section 6.4: the server's timestamp is the next cursor — never substitute the device clock.
        await conn.InsertOrReplaceAsync(new AppSetting
        {
            Key = AppSetting.Keys.LastSyncCursor,
            Value = response.ServerTimestamp.ToString("O"),
        });

        return new SyncResult(conflictedListIds, conflictedItemIds);
    }

    public async Task RefreshReferenceDataIfNeededAsync()
    {
        var conn = await database.GetConnectionAsync();
        var versionSetting = await conn.Table<AppSetting>()
            .Where(s => s.Key == AppSetting.Keys.ReferenceVersion)
            .FirstOrDefaultAsync();

        var serverVersion = await CallAsync(() => referenceApi.GetVersionAsync());
        if (versionSetting?.Value == serverVersion.Version)
        {
            return;
        }

        var products = await CallAsync(() => referenceApi.GetProductsAsync());
        await referenceProductRepository.ReplaceAllAsync(products.Select(p => new ReferenceProduct
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Category = p.Category,
            DefaultUnit = p.DefaultUnit,
        }));

        await conn.InsertOrReplaceAsync(new AppSetting
        {
            Key = AppSetting.Keys.ReferenceVersion,
            Value = serverVersion.Version,
        });
    }

    public Task<AccountResponse> GetAccountAsync()
        => throw new NotImplementedException("Account (Section 6.5) is not wired to a screen in this pass.");

    public Task DeleteAccountAsync()
        => throw new NotImplementedException("Account (Section 6.5) is not wired to a screen in this pass.");

    public Task<AccountExport> ExportAccountDataAsync()
        => throw new NotImplementedException("Account (Section 6.5) is not wired to a screen in this pass.");

    /// <summary>Translates a Refit <see cref="RefitApiException"/> into our typed <see cref="ApiException"/>s (Section 6.7).</summary>
    private static async Task<T> CallAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (RefitApiException ex)
        {
            throw await ApiExceptionMapper.MapAsync(ex);
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkException(ex.Message);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            throw new NetworkException("The request timed out.");
        }
    }

    private static async Task CallAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (RefitApiException ex)
        {
            throw await ApiExceptionMapper.MapAsync(ex);
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkException(ex.Message);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            throw new NetworkException("The request timed out.");
        }
    }
}
