using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;
using StockGuard.Services;
using StockGuard.ViewModels;
using StockGuard.Views;

namespace StockGuard
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");

                    fonts.AddFont(
                        "fa-solid-900.ttf",
                        "FontAwesome");
                });

            // ── Services ──────────────────────────────────────────────────────
            // All services are Singleton — one instance for the app lifetime.
            builder.Services.AddSingleton<FirebaseService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<QrPrintService>();

            // ── ViewModels ────────────────────────────────────────────────────
            //
            // Transient  → new instance on every navigation (no state survives)
            // Singleton  → same instance for app lifetime (state survives navigation)
            //
            // Pages that are FlyoutItems AND have filters/search state that the
            // user expects to persist across back-navigation must be Singleton.
            //
            // ToolDetailsViewModel       → Singleton: filter state must survive QR navigation
            // TransactionHistoryViewModel → Singleton: filter state must survive back-navigation
            //
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<WorkerDashboardViewModel>();
            builder.Services.AddSingleton<ToolDetailsViewModel>();           // ← Singleton
            builder.Services.AddTransient<PEDashboardViewModel>();
            builder.Services.AddSingleton<TransactionHistoryViewModel>();    // ← Singleton (was Transient)
            builder.Services.AddTransient<BorrowRequestsViewModel>();
            builder.Services.AddTransient<WorkerManagementViewModel>();
            builder.Services.AddTransient<EquipmentCatalogViewModel>();
            builder.Services.AddTransient<ToolListViewModel>();
            builder.Services.AddTransient<QrDisplayViewModel>();
            builder.Services.AddTransient<DamageReportsViewModel>();
            builder.Services.AddTransient<ProjectManagementViewModel>();
            builder.Services.AddTransient<ProjectDetailsViewModel>();
            builder.Services.AddTransient<BulkSelectViewModel>();
            builder.Services.AddTransient<PauseRequestsViewModel>();
            builder.Services.AddTransient<ProjectAnalyticsViewModel>();
            builder.Services.AddTransient<WorkerToolDetailsViewModel>();
            builder.Services.AddTransient<AdminToolDetailsViewModel>();

            // ── Views ─────────────────────────────────────────────────────────
            //
            // Views must match their ViewModel lifetime.
            // A Singleton ViewModel paired with a Transient View means the View
            // is recreated but BindingContext points to the same VM instance —
            // this causes binding re-subscription overhead and potential
            // double-render on every navigation. Keep them in sync.
            //
            builder.Services.AddTransient<LoginView>();
            builder.Services.AddTransient<RegisterView>();
            builder.Services.AddTransient<WorkerDashboardView>();
            builder.Services.AddTransient<MainView>();
            builder.Services.AddTransient<QrScannerView>();
            builder.Services.AddSingleton<ToolDetailsView>();               // ← Singleton
            builder.Services.AddTransient<PEDashboardView>();
            builder.Services.AddTransient<BorrowRequestsView>();
            builder.Services.AddSingleton<TransactionHistoryView>();        // ← Singleton (was Transient)
            builder.Services.AddTransient<WorkerManagementView>();
            builder.Services.AddTransient<EquipmentCatalogView>();
            builder.Services.AddTransient<ToolListView>();
            builder.Services.AddTransient<QrDisplayView>();
            builder.Services.AddTransient<DamageReportsView>();
            builder.Services.AddTransient<ProjectManagementView>();
            builder.Services.AddTransient<ProjectDetailsView>();
            builder.Services.AddTransient<ProjectAnalyticsView>();
            builder.Services.AddTransient<BulkSelectView>();
            builder.Services.AddTransient<PauseRequestsView>();
            builder.Services.AddTransient<WorkerToolDetailsView>();
            builder.Services.AddTransient<AdminToolDetailsView>();

            // ── Shell ─────────────────────────────────────────────────────────
            builder.Services.AddSingleton<AppShell>();

            builder.Services.AddSingleton<NotificationState>(
                _ => NotificationState.Instance);
            builder.Services.AddSingleton<NotificationSyncService>();
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<HomeView>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}