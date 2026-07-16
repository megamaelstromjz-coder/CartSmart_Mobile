using CartSmart.Mobile.Views;

namespace CartSmart.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Detail/modal routes navigated to via Shell.Current.GoToAsync — not top-level tabs.
        Routing.RegisterRoute(nameof(ListDetailPage), typeof(ListDetailPage));
        Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
    }
}
