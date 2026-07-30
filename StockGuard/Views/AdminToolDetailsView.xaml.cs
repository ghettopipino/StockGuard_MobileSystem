using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class AdminToolDetailsView : ContentPage
{
    public AdminToolDetailsView(AdminToolDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    /// <summary>
    /// Code-behind fallback for the ← back button.
    /// Guards against silent async exceptions in the ViewModel command.
    /// </summary>
    private async void OnBackClicked(object sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync(".."); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[AdminToolDetails] Back error: {ex.Message}");
        }
    }
}