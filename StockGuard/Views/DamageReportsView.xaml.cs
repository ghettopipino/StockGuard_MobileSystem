using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class DamageReportsView : ContentPage
{
    public DamageReportsView(DamageReportsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DamageReportsViewModel vm)
            await vm.LoadReportsAsync();
    }
}