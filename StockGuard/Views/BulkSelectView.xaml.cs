using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class BulkSelectView : ContentPage
{
    public BulkSelectView(BulkSelectViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BulkSelectViewModel vm)
            await vm.LoadAsync();
    }
}