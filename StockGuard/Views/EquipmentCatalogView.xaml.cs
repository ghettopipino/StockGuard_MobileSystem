using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class EquipmentCatalogView : ContentPage
{
    public EquipmentCatalogView(EquipmentCatalogViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is EquipmentCatalogViewModel vm)
            await vm.LoadCatalogsAsync();
    }
}