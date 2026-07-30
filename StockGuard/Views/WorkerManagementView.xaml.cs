using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class WorkerManagementView : ContentPage
{
    public WorkerManagementView(WorkerManagementViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is WorkerManagementViewModel vm)
            await vm.LoadWorkersAsync();
    }
}