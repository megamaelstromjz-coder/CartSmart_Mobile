using CartSmart.Mobile.ViewModels;
using CartSmart.Mobile.Views;

namespace CartSmart.Mobile;

public partial class AppShell : Shell
{
    private readonly AppShellViewModel _viewModel;

    public AppShell(AppShellViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // Detail/modal routes navigated to via Shell.Current.GoToAsync — not top-level flyout items.
        Routing.RegisterRoute(nameof(ListDetailPage), typeof(ListDetailPage));
        Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));

        Navigated += OnShellNavigated;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshCommand.ExecuteAsync(null);
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var location = e.Current?.Location;
        if (location is null)
        {
            return;
        }

        var segments = location.OriginalString.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 0)
        {
            _viewModel.CurrentRoute = segments[0];
        }
    }
}
