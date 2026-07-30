using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class RegisterView : ContentPage
{
    public RegisterView(RegisterViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
