using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class ResetPasswordView : ContentPage
{
    public ResetPasswordView(
        ResetPasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}