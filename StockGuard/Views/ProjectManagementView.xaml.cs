using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class ProjectManagementView : ContentPage
{
    public ProjectManagementView(
        ProjectManagementViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProjectManagementViewModel vm)
            await vm.LoadProjectsAsync();
    }
}