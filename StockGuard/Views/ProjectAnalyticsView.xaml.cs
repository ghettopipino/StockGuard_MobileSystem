using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class ProjectAnalyticsView : ContentPage
{
    public ProjectAnalyticsView(ProjectAnalyticsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProjectAnalyticsViewModel vm)
            await vm.LoadAsync();
    }

    // NOTE: OnGoBackClicked removed.
    // ProjectAnalyticsView is a FlyoutItem root page.
    // GoToAsync("..") on a root page does nothing.
    // Navigation is handled by OpenFlyoutCommand (☰) in the header,
    // inherited from BaseViewModel.
}