using StockGuard.ViewModels;

namespace StockGuard.Views;

/// <summary>
/// NO [QueryProperty] here.
/// They live on ToolListViewModel only. Shell sets them on the BindingContext
/// automatically. Declaring them here too causes a double-apply that corrupts
/// the navigation stack and breaks GoToAsync("..").
/// </summary>
public partial class ToolListView : ContentPage
{
    public ToolListView(ToolListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    /// <summary>
    /// Code-behind fallback for the ← back button.
    ///
    /// WHY THIS EXISTS:
    /// new Command(async () => ...) silently swallows async exceptions.
    /// If anything inside GoToAsync("..") throws, the ViewModel command
    /// completes with no error — the user just sees "nothing happened."
    ///
    /// A Clicked event handler runs directly on the UI thread and surfaces
    /// exceptions properly, making navigation reliable regardless of what
    /// the ViewModel command does.
    ///
    /// Only one of these fires per tap in practice. Having both is safe.
    /// </summary>
    private async void OnBackClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ToolListView] Back navigation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Shell guarantees [QueryProperty] setters on the BindingContext fire
    /// before OnAppearing, so CatalogId is already populated here.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ToolListViewModel vm)
            await vm.LoadToolsAsync();
    }
}