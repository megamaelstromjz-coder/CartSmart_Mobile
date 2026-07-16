using CartSmart.Mobile.ViewModels;

namespace CartSmart.Mobile.Views;

public partial class SuggestionsPage : ContentPage
{
    private readonly SuggestionsViewModel _viewModel;

    public SuggestionsPage(SuggestionsViewModel viewModel)
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
