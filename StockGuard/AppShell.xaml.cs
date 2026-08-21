using StockGuard.Services;
using StockGuard.Views;
using StockGuard.ViewModels;

namespace StockGuard;

public partial class AppShell : Shell
{
    private readonly AuthService _auth;
    private readonly NotificationSyncService _notifSync;
    private Button[] _navButtons = Array.Empty<Button>();

    public AppShell(AuthService auth, NotificationSyncService notifSync, IServiceProvider services)
    {
        _auth = auth;
        _notifSync = notifSync;
        InitializeComponent();
        var homeView = services.GetRequiredService<HomeView>();
        var homeVm = services.GetRequiredService<HomeViewModel>();
        homeView.BindingContext = homeVm;
        RegisterDetailRoutes();

        // Bind flyout badges to NotificationState singleton
        BindingContext = NotificationState.Instance;

        _navButtons = new[]
        {
            //BtnDashboard,
            BtnProjects,     BtnCatalog,
            BtnTools,     BtnWorkers,      BtnPause,
            BtnDamage,    BtnTransactions,
            //BtnAnalytics
        };

        //SetActive(BtnDashboard);
    }

    // ── Route registration ────────────────────────────────────────────────────
    //
    // RULE: Only detail/sub-pages go here.
    //       FlyoutItem pages are auto-routed by Shell — never register them here.
    //
    // FlyoutItem pages (NEVER register):
    //   LoginView, WorkerDashboardView, PEDashboardView,
    //   ProjectManagementView, EquipmentCatalogView, ToolDetailsView,
    //   WorkerManagementView, PauseRequestsView, DamageReportsView,
    //   TransactionHistoryView, ProjectAnalyticsView
    //
    // Detail pages (registered below):
    //   Everything that is navigated to with GoToAsync("PageName?param=...")
    //   and popped with GoToAsync("..").
    private static void RegisterDetailRoutes()
    {
        // ── Auth ──────────────────────────────────────────────────────────────
        Routing.RegisterRoute(nameof(RegisterView), typeof(RegisterView));

        // ── QR ────────────────────────────────────────────────────────────────
        Routing.RegisterRoute(nameof(QrScannerView), typeof(QrScannerView));
        Routing.RegisterRoute(nameof(QrDisplayView), typeof(QrDisplayView));

        // ── QR scan destinations (role-based) ─────────────────────────────────
        // Worker scans QR  → WorkerToolDetailsView (actions: borrow, pause, etc.)
        // Admin  scans QR  → AdminToolDetailsView  (read-only: info + history)
        // Both are registered routes so GoToAsync("..") pops them correctly.
        Routing.RegisterRoute(nameof(WorkerToolDetailsView), typeof(WorkerToolDetailsView));
        Routing.RegisterRoute(nameof(AdminToolDetailsView), typeof(AdminToolDetailsView));

        // NOTE: ToolDetailsView is NOT registered here.
        // It is a FlyoutItem in AppShell.xaml (the admin tool browser).
        // Registering it here too would push it as a modal ON TOP of Shell,
        // making the sidebar disappear. Navigate to it with //ToolDetailsView.

        // ── Equipment catalog detail ───────────────────────────────────────────
        Routing.RegisterRoute(nameof(ToolListView), typeof(ToolListView));

        // ── Borrow requests ───────────────────────────────────────────────────
        Routing.RegisterRoute(nameof(BorrowRequestsView), typeof(BorrowRequestsView));

        // ── Project details ───────────────────────────────────────────────────
        Routing.RegisterRoute(nameof(ProjectDetailsView), typeof(ProjectDetailsView));

        // ── Bulk select ───────────────────────────────────────────────────────
        Routing.RegisterRoute(nameof(BulkSelectView), typeof(BulkSelectView));
    }

    // ── Active item highlight ─────────────────────────────────────────────────
    private void SetActive(Button active)
    {
        foreach (var btn in _navButtons)
        {
            btn.BackgroundColor = Colors.Transparent;
            btn.TextColor = Color.FromArgb("#94a3b8");
        }
        active.BackgroundColor = Color.FromArgb("#1e3a5f");
        active.TextColor = Color.FromArgb("#60a5fa");
    }

    // ── Nav helper ────────────────────────────────────────────────────────────
    private async Task NavigateTo(string absoluteRoute, Button sender)
    {
        SetActive(sender);
        FlyoutIsPresented = false;
        await GoToAsync(absoluteRoute);
    }

    // ── Flyout handlers ───────────────────────────────────────────────────────
    //private async void OnDashboardClicked(object s, EventArgs e)
    //    => await NavigateTo("//PEDashboardView", BtnDashboard);

    private async void OnProjectsClicked(object s, EventArgs e)
        => await NavigateTo("//ProjectManagementView", BtnProjects);

    private async void OnCatalogClicked(object s, EventArgs e)
        => await NavigateTo("//EquipmentCatalogView", BtnCatalog);

    private async void OnToolsClicked(object s, EventArgs e)
        => await NavigateTo("//ToolDetailsView", BtnTools);

    private async void OnWorkersClicked(object s, EventArgs e)
        => await NavigateTo("//WorkerManagementView", BtnWorkers);

    private async void OnPauseClicked(object s, EventArgs e)
        => await NavigateTo("//PauseRequestsView", BtnPause);

    private async void OnDamageClicked(object s, EventArgs e)
        => await NavigateTo("//DamageReportsView", BtnDamage);

    private async void OnTransactionsClicked(object s, EventArgs e)
        => await NavigateTo("//TransactionHistoryView", BtnTransactions);

    //private async void OnAnalyticsClicked(object s, EventArgs e)
    //    => await NavigateTo("//ProjectAnalyticsView", BtnAnalytics);

    // ── Logout ────────────────────────────────────────────────────────────────
    private async void OnLogoutClicked(object s, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Logout", "Are you sure you want to logout?",
            "Logout", "Cancel");

        if (!confirm) return;

        FlyoutIsPresented = false;
        _auth.Logout();
        await GoToAsync("//LoginView");
    }
}