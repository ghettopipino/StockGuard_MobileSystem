using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class PEDashboardView : ContentPage
{
    public PEDashboardView(PEDashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PEDashboardViewModel vm)
            await vm.RefreshOnAppearingAsync();
    }
    
}