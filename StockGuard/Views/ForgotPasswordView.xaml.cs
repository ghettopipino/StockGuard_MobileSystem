using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class ForgotPasswordView : ContentPage
{
    public ForgotPasswordView(
        ForgotPasswordViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}