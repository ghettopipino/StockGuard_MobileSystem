using StockGuard.ViewModels;

namespace StockGuard.Views;

/// <summary>
/// Worker-facing tool detail page.
/// Receives toolId via [QueryProperty] on the ViewModel.
/// Navigated to from WorkerDashboardViewModel.ViewToolAsync
/// and from QrScannerView after a successful scan.
/// </summary>
public partial class WorkerToolDetailsView : ContentPage
{
    public WorkerToolDetailsView(WorkerToolDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    /// <summary>
    /// Code-behind fallback for the ← back button.
    /// Guarantees navigation even if the ViewModel command
    /// silently swallows an async exception.
    /// </summary>
    private async void OnBackClicked(object sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync(".."); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[WorkerToolDetailsView] Back error: {ex.Message}");
        }
    }
}