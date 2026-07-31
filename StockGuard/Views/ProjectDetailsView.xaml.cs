using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class ProjectDetailsView : ContentPage
{
    public ProjectDetailsView(ProjectDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private void OnDeployViaScanClicked(object sender, EventArgs e)
    => System.Diagnostics.Debug.WriteLine("[Deploy via Scan] tapped");

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProjectDetailsViewModel vm)
            await vm.LoadAsync();
    }


}