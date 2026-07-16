using CartSmart.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartSmart.Mobile.ViewModels;

/// <summary>Backs the custom sidebar flyout in <see cref="CartSmart.Mobile.AppShell"/>.</summary>
public partial class AppShellViewModel(ISuggestionSignalProvider suggestionSignalProvider) : ObservableObject
{
    [ObservableProperty]
    private int suggestionCount;

    [ObservableProperty]
    private string currentRoute = "ListsPage";

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var signals = await suggestionSignalProvider.GetSignalsAsync();
        SuggestionCount = signals.Count;
    }

    [RelayCommand]
    private static async Task NavigateAsync(string route)
    {
        Shell.Current.FlyoutIsPresented = false;
        await Shell.Current.GoToAsync($"//{route}");
    }
}
