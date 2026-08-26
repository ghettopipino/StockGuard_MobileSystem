using StockGuard.Services;
using StockGuard.Views;
using StockGuard.ViewModels;

namespace StockGuard;

public partial class AppShell : Shell
{
    private readonly AuthService _auth;
    private readonly NotificationSyncService _notifSync;

    private Button[] _navButtons = Array.Empty<Button>();

    // ─────────────────────────────────────────────────────────────
    // CONSTRUCTOR
    // ─────────────────────────────────────────────────────────────

    public AppShell(
        AuthService auth,
        NotificationSyncService notifSync,
        IServiceProvider services)
    {
        _auth = auth;
        _notifSync = notifSync;

        InitializeComponent();

        // ── Home ─────────────────────────────────────────────

        var homeView =
            services.GetRequiredService<HomeView>();

        var homeVm =
            services.GetRequiredService<HomeViewModel>();

        homeView.BindingContext = homeVm;

        // ── Routes ───────────────────────────────────────────

        RegisterDetailRoutes();

        // ── Notification State ───────────────────────────────

        BindingContext =
            NotificationState.Instance;

        // ── Flyout Navigation Buttons ────────────────────────

        _navButtons = new[]
        {
            BtnDashboard,
            BtnProjects,
            BtnCatalog,
            BtnTools,
            BtnWorkers,
            BtnPause,
            BtnDamage,
            BtnTransactions,
            BtnAnalytics,
        };

        // Load current notification counts
        // and start Firebase synchronization.
        _ = InitializeNotificationsAsync();
    }

    // ─────────────────────────────────────────────────────────────
    // NOTIFICATIONS
    // ─────────────────────────────────────────────────────────────

    private async Task InitializeNotificationsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine(
                "[AppShell] Initializing notifications...");

            // Load existing pending counts.
            await _notifSync.RefreshAsync();

            // Start listening for Firebase changes.
            _notifSync.StartLiveSync();

            System.Diagnostics.Debug.WriteLine(
                "[AppShell] Notification sync started.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[AppShell] Notification initialization error: " +
                $"{ex.Message}");
        }
    }

    private async Task RefreshNotificationsAsync()
    {
        try
        {
            await _notifSync.RefreshAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[AppShell] Notification refresh error: " +
                $"{ex.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // ROUTE REGISTRATION
    // ─────────────────────────────────────────────────────────────

    private static void RegisterDetailRoutes()
    {
        // ── Authentication ───────────────────────────────────

        Routing.RegisterRoute(
            nameof(RegisterView),
            typeof(RegisterView));

        // ── QR ───────────────────────────────────────────────

        Routing.RegisterRoute(
            nameof(QrScannerView),
            typeof(QrScannerView));

        Routing.RegisterRoute(
            nameof(QrDisplayView),
            typeof(QrDisplayView));

        // ── QR Scan Destinations ─────────────────────────────

        Routing.RegisterRoute(
            nameof(WorkerToolDetailsView),
            typeof(WorkerToolDetailsView));

        Routing.RegisterRoute(
            nameof(AdminToolDetailsView),
            typeof(AdminToolDetailsView));

        // ── Equipment Catalog Detail ─────────────────────────

        Routing.RegisterRoute(
            nameof(ToolListView),
            typeof(ToolListView));

        // ── Borrow Requests ──────────────────────────────────

        Routing.RegisterRoute(
            nameof(BorrowRequestsView),
            typeof(BorrowRequestsView));

        // ── Project Details ──────────────────────────────────

        Routing.RegisterRoute(
            nameof(ProjectDetailsView),
            typeof(ProjectDetailsView));

        // ── Bulk Select ──────────────────────────────────────

        Routing.RegisterRoute(
            nameof(BulkSelectView),
            typeof(BulkSelectView));
    }

    // ─────────────────────────────────────────────────────────────
    // ACTIVE NAVIGATION ITEM
    // ─────────────────────────────────────────────────────────────

    private void SetActive(Button active)
    {
        foreach (var button in _navButtons)
        {
            button.BackgroundColor =
                Colors.Transparent;

            button.TextColor =
                Color.FromArgb("#94a3b8");
        }

        active.BackgroundColor =
            Color.FromArgb("#1e3a5f");

        active.TextColor =
            Color.FromArgb("#60a5fa");
    }

    // ─────────────────────────────────────────────────────────────
    // NAVIGATION
    // ─────────────────────────────────────────────────────────────

    private async Task NavigateTo(
        string absoluteRoute,
        Button sender)
    {
        SetActive(sender);

        FlyoutIsPresented = false;

        await GoToAsync(
            absoluteRoute);
    }

    // ─────────────────────────────────────────────────────────────
    // DASHBOARD
    // ─────────────────────────────────────────────────────────────

    private async void OnDashboardClicked(
        object sender,
        EventArgs e)
    {
        await RefreshNotificationsAsync();

        await NavigateTo(
            "//PEDashboardView",
            BtnDashboard);
    }

    // ─────────────────────────────────────────────────────────────
    // PROJECTS
    // ─────────────────────────────────────────────────────────────

    private async void OnProjectsClicked(
        object sender,
        EventArgs e)
    {
        await NavigateTo(
            "//ProjectManagementView",
            BtnProjects);
    }

    // ─────────────────────────────────────────────────────────────
    // CATALOG
    // ─────────────────────────────────────────────────────────────

    private async void OnCatalogClicked(
        object sender,
        EventArgs e)
    {
        await NavigateTo(
            "//EquipmentCatalogView",
            BtnCatalog);
    }

    // ─────────────────────────────────────────────────────────────
    // EQUIPMENT
    // ─────────────────────────────────────────────────────────────

    private async void OnToolsClicked(
        object sender,
        EventArgs e)
    {
        await NavigateTo(
            "//ToolDetailsView",
            BtnTools);
    }

    // ─────────────────────────────────────────────────────────────
    // WORKERS
    // ─────────────────────────────────────────────────────────────

    private async void OnWorkersClicked(
        object sender,
        EventArgs e)
    {
        await RefreshNotificationsAsync();

        await NavigateTo(
            "//WorkerManagementView",
            BtnWorkers);
    }

    // ─────────────────────────────────────────────────────────────
    // RETURN & CHECK-IN
    // ─────────────────────────────────────────────────────────────
    //
    // The internal page/route name remains PauseRequestsView
    // because it comes from the original Pause workflow.
    //
    // The actual current UI handles:
    // - End-Day Check-In verification
    // - Formal Return requests

    private async void OnPauseClicked(
        object sender,
        EventArgs e)
    {
        await RefreshNotificationsAsync();

        await NavigateTo(
            "//PauseRequestsView",
            BtnPause);
    }

    // ─────────────────────────────────────────────────────────────
    // DAMAGE REPORTS
    // ─────────────────────────────────────────────────────────────

    private async void OnDamageClicked(
        object sender,
        EventArgs e)
    {
        await RefreshNotificationsAsync();

        await NavigateTo(
            "//DamageReportsView",
            BtnDamage);
    }

    // ─────────────────────────────────────────────────────────────
    // TRANSACTIONS
    // ─────────────────────────────────────────────────────────────

    private async void OnTransactionsClicked(
        object sender,
        EventArgs e)
    {
        await RefreshNotificationsAsync();

        await NavigateTo(
            "//TransactionHistoryView",
            BtnTransactions);
    }

    // ─────────────────────────────────────────────────────────────
    // PROJECT ANALYTICS
    // ─────────────────────────────────────────────────────────────

    private async void OnAnalyticsClicked(
        object sender,
        EventArgs e)
    {
        await NavigateTo(
            "//ProjectAnalyticsView",
            BtnAnalytics);
    }

    // ─────────────────────────────────────────────────────────────
    // LOGOUT
    // ─────────────────────────────────────────────────────────────

    private async void OnLogoutClicked(
        object sender,
        EventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "Logout",
                "Are you sure you want to logout?",
                "Logout",
                "Cancel");

        if (!confirm)
            return;

        FlyoutIsPresented = false;

        _auth.Logout();

        await GoToAsync(
            "//LoginView");
    }
}