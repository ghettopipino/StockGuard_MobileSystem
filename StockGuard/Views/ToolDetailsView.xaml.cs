using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class ToolDetailsView : ContentPage
{
    public ToolDetailsView(ToolDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ToolDetailsViewModel vm)
            await vm.LoadAsync(forceRefresh: true);
    }
}