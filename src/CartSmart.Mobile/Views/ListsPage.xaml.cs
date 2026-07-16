using CartSmart.Mobile.ViewModels;

namespace CartSmart.Mobile.Views;

public partial class ListsPage : ContentPage
{
    private readonly ListsViewModel _viewModel;

    public ListsPage(ListsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
