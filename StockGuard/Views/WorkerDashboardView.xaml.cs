using StockGuard.ViewModels;

namespace StockGuard.Views;

public partial class WorkerDashboardView : ContentPage
{
    public WorkerDashboardView(WorkerDashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is WorkerDashboardViewModel vm)
            await vm.RefreshOnAppearingAsync();
    }

    // ── Navigate to Borrow Requests page ─────────────────────────
    private async void OnNotificationClicked(
        object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(BorrowRequestsView));
    }
    private async void OnViewHistoryClicked(
    object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(TransactionHistoryView));
    }
}