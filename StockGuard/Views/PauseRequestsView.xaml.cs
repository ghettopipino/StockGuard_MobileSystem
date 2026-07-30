using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class PauseRequestsView : ContentPage
{
    public PauseRequestsView(
        PauseRequestsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PauseRequestsViewModel vm)
            await vm.LoadAsync();
    }
}