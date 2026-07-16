using CartSmart.Mobile.ViewModels;

namespace CartSmart.Mobile.Views;

public partial class ListDetailPage : ContentPage
{
    private readonly ListDetailViewModel _viewModel;

    public ListDetailPage(ListDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnNewItemTextChanged(object? sender, TextChangedEventArgs e)
    {
        await _viewModel.SearchAutocompleteCommand.ExecuteAsync(null);
    }
}
