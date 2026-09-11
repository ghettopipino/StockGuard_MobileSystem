using StockGuard.Services;

namespace StockGuard;

public partial class App : Application
{
    private System.Timers.Timer? _debounce;

    public App(
        AppShell shell,
        ThemeService theme,
        AuthService auth,
        FirebaseService firebase)
    {
        AppDomain.CurrentDomain.UnhandledException +=
            (s, e) =>
                System.Diagnostics.Debug.WriteLine(
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

        // IMPORTANT:
        // Default account seeding is disabled for the
        // final StockGuard system.
        //
        // The first registered Project Engineer will be
        // automatically approved by AuthService.

        StartGlobalSync(firebase);
    }


    // ─────────────────────────────────────────────────────────
    // GLOBAL REAL-TIME SYNC
    // ─────────────────────────────────────────────────────────

    private void StartGlobalSync(
        FirebaseService firebase)
    {
        firebase.StartGlobalListener(
            () =>
            {
                // Debounce — wait 800ms after last change.
                _debounce?.Stop();
                _debounce?.Dispose();

                _debounce =
                    new System.Timers.Timer(800);

                _debounce.AutoReset =
                    false;

                _debounce.Elapsed +=
                    (s, e) =>
                    {
                        MainThread
                            .BeginInvokeOnMainThread(
                                () =>
                                {
                                    try
                                    {
                                        var page =
                                            GetCurrentPage();

                                        if (page is null)
                                            return;


                                        // Skip form pages —
                                        // do not interrupt user input.
                                        var name =
                                            page
                                                .GetType()
                                                .Name;


                                        bool isFormPage =
                                            name.Contains("Create") ||
                                            name.Contains("Edit") ||
                                            name.Contains("Add");


                                        if (isFormPage)
                                            return;


                                        // Current implementation
                                        // intentionally does not
                                        // force page reload here.
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug
                                            .WriteLine(
                                                $"Sync error: " +
                                                $"{ex.Message}");
                                    }
                                });
                    };


                _debounce.Start();
            });
    }


    // ─────────────────────────────────────────────────────────
    // CURRENT PAGE
    // ─────────────────────────────────────────────────────────

    private Page? GetCurrentPage()
    {
        if (MainPage is Shell shell)
        {
            return shell.CurrentPage;
        }


        if (MainPage is NavigationPage nav)
        {
            return nav.CurrentPage;
        }


        return MainPage;
    }
}