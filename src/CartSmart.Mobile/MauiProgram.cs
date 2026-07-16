using CartSmart.Mobile.Api;
using CartSmart.Mobile.Api.Auth;
using CartSmart.Mobile.Data;
using CartSmart.Mobile.Data.Repositories;
using CartSmart.Mobile.Services;
using CartSmart.Mobile.ViewModels;
using CartSmart.Mobile.Views;
using Microsoft.Extensions.Logging;
using Refit;

namespace CartSmart.Mobile;

public static class MauiProgram
{
    // TODO: point at the real CartSmart.Api deployment before shipping; this is the local
    // docker-compose default from the sibling CartSmart.Api repo.
    private static readonly Uri ApiBaseAddress = new("http://localhost:8080");

    public static MauiApp CreateMauiApp()
    {
        // No custom fonts/icons/splash bundled in this source scaffold — add via
        // ConfigureFonts/MauiImage once real assets are dropped into Resources/.
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        RegisterData(builder.Services);
        RegisterApiClients(builder.Services);
        RegisterServices(builder.Services);
        RegisterViewModelsAndViews(builder.Services);

        return builder.Build();
    }

    private static void RegisterData(IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IListRepository, ListRepository>();
        services.AddSingleton<IListItemRepository, ListItemRepository>();
        services.AddSingleton<IReferenceProductRepository, ReferenceProductRepository>();
    }

    private static void RegisterApiClients(IServiceCollection services)
    {
        services.AddSingleton<ITokenStore, SecureStorageTokenStore>();
        services.AddTransient<AuthTokenDelegatingHandler>();

        AddRefitClient<IAuthApi>(services);
        AddRefitClient<IDeviceApi>(services);
        AddRefitClient<IListApi>(services);
        AddRefitClient<ISyncApi>(services);
        AddRefitClient<IAccountApi>(services);
        AddRefitClient<IReferenceApi>(services);
    }

    private static void AddRefitClient<TApi>(IServiceCollection services) where TApi : class
    {
        services
            .AddRefitClient<TApi>()
            .ConfigureHttpClient(client => client.BaseAddress = ApiBaseAddress)
            .AddHttpMessageHandler<AuthTokenDelegatingHandler>();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IDeviceContext, DeviceContext>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<IListService, ListService>();

        // Stubs — DI wiring in place for the next pass (see Services/I*.cs).
        services.AddSingleton<IPredictionService, PredictionService>();
        services.AddSingleton<ISuggestionSignalProvider>(sp => (PredictionService)sp.GetRequiredService<IPredictionService>());
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IShoppingModeService, ShoppingModeService>();
    }

    private static void RegisterViewModelsAndViews(IServiceCollection services)
    {
        services.AddTransient<ListsViewModel>();
        services.AddTransient<ListsPage>();

        services.AddTransient<ListDetailViewModel>();
        services.AddTransient<ListDetailPage>();

        services.AddTransient<ItemDetailViewModel>();
        services.AddTransient<ItemDetailPage>();
    }
}
