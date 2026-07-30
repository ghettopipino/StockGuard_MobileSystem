using StockGuard.ViewModels;
namespace StockGuard.Views;

public partial class TransactionHistoryView : ContentPage
{
    public TransactionHistoryView(TransactionHistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Reset the load guard so re-navigating to this page works correctly
        if (BindingContext is TransactionHistoryViewModel vm)
            vm.ResetLoadState();
    }
}