using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class BorrowRequestsView : ContentPage
{
    public BorrowRequestsView(BorrowRequestsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BorrowRequestsViewModel vm)
            await vm.LoadRequestsAsync();
    }
}