using StockGuard.Services;

namespace StockGuard;

public partial class App : Application
{
    private System.Timers.Timer? _debounce;

    public App(AppShell shell, ThemeService theme,
        AuthService auth, FirebaseService firebase)
    {
        AppDomain.CurrentDomain.UnhandledException +=
            (s, e) => System.Diagnostics.Debug.WriteLine(
                $"UNHANDLED: {e.ExceptionObject}");

        TaskScheduler.UnobservedTaskException +=
            (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine(
                    $"TASK EXCEPTION: {e.Exception}");
                e.SetObserved();
            };

        InitializeComponent();
        theme.Initialize();
        MainPage = shell;

        Task.Run(async () =>
        {
            try
            {
                await auth.SeedDefaultAccountsAsync();
                await firebase.SeedToolsIfEmptyAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Seed error: {ex.Message}");
            }
        });

        // ── Start global real-time sync ────────────────────────
        StartGlobalSync(firebase);
    }

    private void StartGlobalSync(FirebaseService firebase)
    {
        firebase.StartGlobalListener(() =>
        {
            // Debounce — wait 800ms after last change
            _debounce?.Stop();
            _debounce?.Dispose();
            _debounce = new System.Timers.Timer(800);
            _debounce.AutoReset = false;
            _debounce.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        var page = GetCurrentPage();
                        if (page is null) return;

                        // Skip form pages — do not interrupt user input
                        var name = page.GetType().Name;
                        bool isFormPage =
                            name.Contains("Create") ||
                            name.Contains("Edit") ||
                            name.Contains("Add");

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Sync error: {ex.Message}");
                    }
                });
            };
            _debounce.Start();
        });
    }

    private Page? GetCurrentPage()
    {
        if (MainPage is Shell shell)
            return shell.CurrentPage;

        if (MainPage is NavigationPage nav)
            return nav.CurrentPage;

        return MainPage;
    }
}