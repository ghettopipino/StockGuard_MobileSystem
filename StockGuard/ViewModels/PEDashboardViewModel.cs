using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    public class PEDashboardViewModel : BaseViewModel
    {
        private readonly AuthService _auth;
        private readonly ThemeService _theme;
        private readonly FirebaseService _firebase;

        private const int ToolPageSize = 10;
        private const int WorkerPageSize = 10;

        private List<Tool> _allBorrowedTools = new();
        private List<WorkerActivityItem> _allWorkerActivities = new();

        private int _borrowedToolPage;
        private int _workerPage;

        // ─────────────────────────────────────────────────────────
        // IDENTITY
        // ─────────────────────────────────────────────────────────

        public string EngineerName =>
            _auth.CurrentUser?.FullName ??
            "Project Engineer";

        public string EngineerInitials =>
            GetInitials(
                _auth.CurrentUser?.FullName);

        public string GreetingText =>
            GetGreeting();

        public string TodayDate =>
            DateTime.Now.ToString(
                "dddd, MMMM d, yyyy");

        // ─────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────

        public string ThemeIcon =>
     _theme.IsDark
         ? "\uf185"   // Sun
         : "\uf186";  // Moon

        // ─────────────────────────────────────────────────────────
        // STATS
        // ─────────────────────────────────────────────────────────

        private int _totalTools;

        public int TotalTools
        {
            get => _totalTools;
            private set =>
                SetProperty(
                    ref _totalTools,
                    value);
        }

        private int _availableTools;

        public int AvailableTools
        {
            get => _availableTools;
            private set =>
                SetProperty(
                    ref _availableTools,
                    value);
        }

        private int _borrowedTools;

        public int BorrowedTools
        {
            get => _borrowedTools;
            private set =>
                SetProperty(
                    ref _borrowedTools,
                    value);
        }

        private int _damagedTools;

        public int DamagedTools
        {
            get => _damagedTools;
            private set
            {
                SetProperty(
                    ref _damagedTools,
                    value);

                OnPropertyChanged(
                    nameof(HasDamagedTools));
            }
        }

        private int _totalWorkers;

        public int TotalWorkers
        {
            get => _totalWorkers;
            private set =>
                SetProperty(
                    ref _totalWorkers,
                    value);
        }

        private int _pendingReports;

        public int PendingReports
        {
            get => _pendingReports;
            private set
            {
                SetProperty(
                    ref _pendingReports,
                    value);

                OnPropertyChanged(
                    nameof(HasPendingReports));
            }
        }

        // Old property name retained temporarily
        // to avoid breaking existing XAML.
        //
        // It now means:
        // Pending Returns + Pending End-Day Check-Ins.
        private int _pendingReturnCheckInCount;

        public int PendingReturnCheckInCount
        {
            get => _pendingReturnCheckInCount;
            private set
            {
                SetProperty(
                    ref _pendingReturnCheckInCount,
                    value);

                OnPropertyChanged(
                    nameof(HasPendingReturnCheckIn));
            }
        }

        public bool HasPendingReturnCheckIn =>
            PendingReturnCheckInCount > 0;

        public bool HasDamagedTools =>
            DamagedTools > 0;

        public bool HasPendingReports =>
            PendingReports > 0;



        // ─────────────────────────────────────────────────────────
        // BORROWED TOOLS
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<Tool>
            RecentTools
        { get; } = new();

        private bool _hasMoreBorrowedTools;

        public bool HasMoreBorrowedTools
        {
            get => _hasMoreBorrowedTools;
            private set
            {
                SetProperty(
                    ref _hasMoreBorrowedTools,
                    value);

                (LoadMoreBorrowedCommand as Command)?
                    .ChangeCanExecute();
            }
        }

        private string _borrowedToolsLabel =
            string.Empty;

        public string BorrowedToolsLabel
        {
            get => _borrowedToolsLabel;
            private set =>
                SetProperty(
                    ref _borrowedToolsLabel,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // WORKER ACTIVITY
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<WorkerActivityItem>
            WorkerActivities
        { get; } = new();

        private bool _hasMoreWorkers;

        public bool HasMoreWorkers
        {
            get => _hasMoreWorkers;
            private set
            {
                SetProperty(
                    ref _hasMoreWorkers,
                    value);

                (LoadMoreWorkersCommand as Command)?
                    .ChangeCanExecute();
            }
        }

        private string _workerActivityLabel =
            string.Empty;

        public string WorkerActivityLabel
        {
            get => _workerActivityLabel;
            private set =>
                SetProperty(
                    ref _workerActivityLabel,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // DAMAGE PREVIEW
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<DamageReport>
            PendingDamageReports
        { get; } = new();

        // ─────────────────────────────────────────────────────────
        // EMPTY STATES
        // ─────────────────────────────────────────────────────────

        public bool NoBorrowedTools =>
            RecentTools.Count == 0 &&
            !IsBusy;

        public bool NoWorkerActivity =>
            WorkerActivities.Count == 0 &&
            !IsBusy;

        // ─────────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────────

        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set =>
                SetProperty(
                    ref _isRefreshing,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // COMMANDS
        // ─────────────────────────────────────────────────────────

        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand RefreshCommand { get; }

        public ICommand ViewAllToolsCommand { get; }
        public ICommand ViewDamageReportsCommand { get; }
        public ICommand ViewWorkersCommand { get; }
        public ICommand ViewAllTransactionsCommand { get; }
        public ICommand ViewProjectsCommand { get; }

        // Old command name retained temporarily
        // because the actual page is still named
        // PauseRequestsView.
        //
        // That page is now Return & Check-In.
        public ICommand ViewReturnCheckInCommand { get; }
        public ICommand LoadMoreBorrowedCommand { get; }
        public ICommand LoadMoreWorkersCommand { get; }
        public ICommand ScanQrCommand { get; }
        public ICommand OpenFlyoutCommand { get; }

        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public PEDashboardViewModel(
            AuthService auth,
            ThemeService theme,
            FirebaseService firebase)
        {
            _auth = auth;
            _theme = theme;
            _firebase = firebase;

            Title = "PE Dashboard";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(
                    () =>
                        OnPropertyChanged(
                            nameof(ThemeIcon)));

            ToggleThemeCommand =
                new Command(
                    () => _theme.Toggle());

            LogoutCommand =
                new Command(
                    async () =>
                        await LogoutAsync());

            RefreshCommand =
                new Command(
                    async () =>
                        await RefreshAsync());

            ScanQrCommand =
                new Command(
                    async () =>
                        await ScanQrAsync());

            ViewAllToolsCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(
                                "//EquipmentCatalogView"));

            ViewDamageReportsCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(
                                "//DamageReportsView"));

            ViewWorkersCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(
                                "//WorkerManagementView"));

            ViewProjectsCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(
                                "//ProjectManagementView"));

            ViewReturnCheckInCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(
                                "//PauseRequestsView"));

            ViewAllTransactionsCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(
                                $"{nameof(TransactionHistoryView)}" +
                                $"?viewMode=all"));

            LoadMoreBorrowedCommand =
                new Command(
                    execute:
                        LoadNextBorrowedPage,

                    canExecute:
                        () =>
                            HasMoreBorrowedTools);

            LoadMoreWorkersCommand =
                new Command(
                    execute:
                        LoadNextWorkerPage,

                    canExecute:
                        () =>
                            HasMoreWorkers);

            OpenFlyoutCommand =
                 new Command(() =>
                 {
                     if (Shell.Current != null)
                     {
                         Shell.Current.FlyoutIsPresented = true;
                     }
                 });
        }

        // ─────────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────────

        public async Task LoadAsync(
            bool forceRefresh = false)
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var currentUser =
                    _auth.CurrentUser;

                if (currentUser == null)
                {
                    ClearDashboard();
                    return;
                }

                var toolsTask =
                    _firebase.GetAllToolsAsync(
                        forceRefresh);

                var usersTask =
                    _firebase.GetAllUsersAsync();

                var returnsTask =
                    _firebase
                        .GetAllReturnRequestsRawAsync();

                var damageTask =
                    _firebase
                        .GetAllDamageReportsRawAsync();

                var projectsTask =
                    _firebase
                        .GetAllProjectsAsync();

                await Task.WhenAll(
                    toolsTask,
                    usersTask,
                    returnsTask,
                    damageTask,
                    projectsTask);

                var allTools =
                    toolsTask.Result ??
                    new List<Tool>();

                var allUsers =
                    usersTask.Result ??
                    new List<User>();

                var returnRequests =
                    returnsTask.Result ??
                    new List<ReturnRequestResult>();

                var rawReports =
                    damageTask.Result ??
                    new List<DamageReportResult>();

                var projects =
                    projectsTask.Result ??
                    new List<Project>();

                // ───────────────────────────────────────────
                // PROJECTS OWNED BY CURRENT PE
                // ───────────────────────────────────────────

                var myProjects =
                    projects
                        .Where(p =>
                            !p.IsDeleted &&
                            p.CreatedBy ==
                                currentUser.UniqueKey)
                        .ToList();

                var myProjectIds =
                    myProjects
                        .Select(p =>
                            p.ProjectId)
                        .ToHashSet();

                // ───────────────────────────────────────────
                // TOOLS FOR THIS PE'S PROJECTS
                // ───────────────────────────────────────────
                //
                // Available equipment is company inventory,
                // so Total / Available still use all tools.
                //
                // Borrowed / pending / damaged project
                // activity is filtered to this PE's projects.

                var myProjectTools =
                    allTools
                        .Where(t =>
                            myProjectIds.Contains(
                                t.BorrowedProjectId))
                        .ToList();

                // ───────────────────────────────────────────
                // STATS
                // ───────────────────────────────────────────

                TotalTools =
                    allTools.Count;

                AvailableTools =
                    allTools.Count(t =>
                        t.Status ==
                        "Available");

                BorrowedTools =
    myProjectTools.Count(t =>
        t.Status == "Borrowed" ||
        t.Status == "PendingReturn");

                DamagedTools =
                    myProjectTools.Count(t =>
                        t.Status ==
                            "Damaged" ||
                        t.Status ==
                            "UnderRepair");

                // ───────────────────────────────────────────
                // WORKERS
                // ───────────────────────────────────────────

                var approvedWorkers =
                    allUsers
                        .Where(u =>
                            u.Role ==
                                "Worker" &&
                            u.AccountStatus ==
                                "Approved")
                        .ToList();

                var pendingWorkerCount =
                    allUsers.Count(u =>
                        u.Role ==
                            "Worker" &&
                        u.AccountStatus ==
                            "Pending");

                // Workers assigned to any of this PE's projects.
                var myWorkerKeys =
                    new HashSet<string>();

                foreach (var project in myProjects)
                {
                    var keys =
                        await _firebase
                            .GetProjectWorkerKeysAsync(
                                project.ProjectId);

                    foreach (var key in keys)
                    {
                        myWorkerKeys.Add(key);
                    }
                }

                var myApprovedWorkers =
                    approvedWorkers
                        .Where(w =>
                            myWorkerKeys.Contains(
                                w.UniqueKey))
                        .ToList();

                TotalWorkers =
                    myApprovedWorkers.Count;

                // ───────────────────────────────────────────
                // RETURN + CHECK-IN PENDING
                // ───────────────────────────────────────────

                var pendingReturns =
                    returnRequests.Count(r =>
                        r.Request.Status ==
                            "Pending" &&
                        myProjectIds.Contains(
                            r.Request.ProjectId));

                var pendingCheckIns =
                    allTools.Count(t =>
                        t.Status ==
                            "Borrowed" &&
                        t.IsCheckInPending &&
                        myProjectIds.Contains(
                            t.BorrowedProjectId));

                PendingReturnCheckInCount =
                    pendingReturns +
                    pendingCheckIns;

                // ───────────────────────────────────────────
                // DAMAGE
                // ───────────────────────────────────────────

                var myDamageReports =
                    rawReports
                        .Where(r =>
                            myProjectIds.Contains(
                                r.Report.ProjectId))
                        .ToList();

                var pendingDamageCount =
                    myDamageReports.Count(r =>
                        r.Report.Status ==
                        "Pending");

                // Pending workers remain global account
                // approvals for now.
                PendingReports =
                     pendingDamageCount +
                     pendingWorkerCount +
                     PendingReturnCheckInCount;

                // ───────────────────────────────────────────
                // DAMAGE PREVIEW
                // ───────────────────────────────────────────

                PendingDamageReports.Clear();

                foreach (var item in
                    myDamageReports
                        .Where(r =>
                            r.Report.Status ==
                            "Pending")
                        .OrderByDescending(r =>
                            r.Report.ReportDate)
                        .Take(5))
                {
                    PendingDamageReports.Add(
                        item.Report);
                }

                // ───────────────────────────────────────────
                // BORROWED TOOLS PREVIEW
                // ───────────────────────────────────────────

                BorrowedTools =
     allTools.Count(t =>
         !t.IsDeleted &&
         (t.Status == "Borrowed" ||
          t.Status == "PendingReturn"));

                _borrowedToolPage =
                    0;

                RecentTools.Clear();

                AppendBorrowedPage();

                // ───────────────────────────────────────────
                // WORKER ACTIVITY
                // ───────────────────────────────────────────

                _allWorkerActivities =
                    myApprovedWorkers
                        .Select(worker =>
                            new WorkerActivityItem
                            {
                                WorkerName =
                                    worker.FullName,

                                WorkerInitials =
                                    GetInitials(
                                        worker.FullName),

                                AssignedTools =
                                    myProjectTools.Count(t =>
                                        t.AssignedWorkerId ==
                                        worker.UniqueKey),

                                Status =
                                    myProjectTools.Any(t =>
                                        t.AssignedWorkerId ==
                                        worker.UniqueKey)
                                        ? "Active"
                                        : "Idle"
                            })
                        .OrderByDescending(w =>
                            w.AssignedTools)
                        .ToList();

                _workerPage =
                    0;

                WorkerActivities.Clear();

                AppendWorkerPage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PEDashboardVM] Load error: " +
                    $"{ex.Message}");
            }
            finally
            {
                IsBusy = false;

                NotifyEmptyStates();
            }
        }

        // ─────────────────────────────────────────────────────────
        // BORROWED PAGINATION
        // ─────────────────────────────────────────────────────────

        private void AppendBorrowedPage()
        {
            var slice =
                _allBorrowedTools
                    .Skip(
                        _borrowedToolPage *
                        ToolPageSize)
                    .Take(
                        ToolPageSize);

            foreach (var tool in slice)
            {
                RecentTools.Add(tool);
            }

            UpdateBorrowedPaginationState();
        }

        private void LoadNextBorrowedPage()
        {
            if (!HasMoreBorrowedTools)
                return;

            _borrowedToolPage++;

            AppendBorrowedPage();
        }

        private void UpdateBorrowedPaginationState()
        {
            int visible =
                RecentTools.Count;

            int total =
                _allBorrowedTools.Count;

            HasMoreBorrowedTools =
                visible < total;

            BorrowedToolsLabel =
                total == 0
                    ? string.Empty
                    : $"Showing {visible} of " +
                      $"{total} borrowed tools";
        }

        // ─────────────────────────────────────────────────────────
        // WORKER PAGINATION
        // ─────────────────────────────────────────────────────────

        private void AppendWorkerPage()
        {
            var slice =
                _allWorkerActivities
                    .Skip(
                        _workerPage *
                        WorkerPageSize)
                    .Take(
                        WorkerPageSize);

            foreach (var item in slice)
            {
                WorkerActivities.Add(item);
            }

            UpdateWorkerPaginationState();
        }

        private void LoadNextWorkerPage()
        {
            if (!HasMoreWorkers)
                return;

            _workerPage++;

            AppendWorkerPage();
        }

        private void UpdateWorkerPaginationState()
        {
            int visible =
                WorkerActivities.Count;

            int total =
                _allWorkerActivities.Count;

            HasMoreWorkers =
                visible < total;

            WorkerActivityLabel =
                total == 0
                    ? string.Empty
                    : $"Showing {visible} of " +
                      $"{total} workers";
        }

        // ─────────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────────

        public async Task RefreshOnAppearingAsync()
        {
            await LoadAsync(
                forceRefresh: false);
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;

            try
            {
                await LoadAsync(
                    forceRefresh: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // QR SCAN
        // ─────────────────────────────────────────────────────────

        private async Task ScanQrAsync()
        {
            try
            {
                await Shell.Current
                    .GoToAsync(
                        nameof(QrScannerView));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    ex.Message,
                    "OK");
            }
        }

        // ─────────────────────────────────────────────────────────
        // LOGOUT
        // ─────────────────────────────────────────────────────────

        private async Task LogoutAsync()
        {
            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Logout",
                    "Are you sure you want to logout?",
                    "Logout",
                    "Cancel");

            if (!confirm)
                return;

            _auth.Logout();

            await Shell.Current
                .GoToAsync(
                    "//LoginView");
        }

        // ─────────────────────────────────────────────────────────
        // CLEAR
        // ─────────────────────────────────────────────────────────

        private void ClearDashboard()
        {
            TotalTools = 0;
            AvailableTools = 0;
            BorrowedTools = 0;
            DamagedTools = 0;
            TotalWorkers = 0;
            PendingReports = 0;
            PendingReturnCheckInCount = 0;

            RecentTools.Clear();
            WorkerActivities.Clear();
            PendingDamageReports.Clear();

            _allBorrowedTools.Clear();
            _allWorkerActivities.Clear();

            BorrowedToolsLabel =
                string.Empty;

            WorkerActivityLabel =
                string.Empty;

            HasMoreBorrowedTools =
                false;

            HasMoreWorkers =
                false;
        }

        // ─────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────

        private void NotifyEmptyStates()
        {
            OnPropertyChanged(
                nameof(NoBorrowedTools));

            OnPropertyChanged(
                nameof(NoWorkerActivity));
        }

        private static string GetInitials(
            string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "PE";

            var parts =
                name
                    .Trim()
                    .Split(
                        ' ',
                        StringSplitOptions
                            .RemoveEmptyEntries);

            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}"
                    .ToUpper()
                : name[0]
                    .ToString()
                    .ToUpper();
        }

        private static string GetGreeting()
        {
            var hour =
                DateTime.Now.Hour;

            return hour < 12
                ? "Good morning"
                : hour < 17
                    ? "Good afternoon"
                    : "Good evening";
        }
    }
}