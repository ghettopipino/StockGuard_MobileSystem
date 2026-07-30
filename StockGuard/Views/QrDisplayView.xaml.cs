using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class QrDisplayView : ContentPage
{
    public QrDisplayView(QrDisplayViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}