using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class HomeView : ContentPage
{
    public HomeView(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm; // ← this must be here
    }
}