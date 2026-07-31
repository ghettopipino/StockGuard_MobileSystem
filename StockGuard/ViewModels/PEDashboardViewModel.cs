using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    /// <summary>
    /// Optimized PE Dashboard ViewModel.
    ///
    /// Key performance changes vs original:
    ///   1. Firebase calls run in PARALLEL via Task.WhenAll — was sequential.
    ///      NOTE: GetAllUsersAsync() has no forceRefresh overload, so it always
    ///      hits Firebase. The other three calls respect their caches.
    ///   2. RecentTools (borrowed tools) paginates in slices of 10.
    ///   3. WorkerActivities paginates in slices of 10.
    ///   4. XAML uses nested non-scrolling CollectionViews inside the Header
    ///      of an outer CollectionView — only visible rows are inflated.
    ///   5. IsBusy guard prevents parallel re-entrant load calls.
    /// </summary>
    public class PEDashboardViewModel : BaseViewModel
    {
        private readonly AuthService _auth;
        private readonly ThemeService _theme;
        private readonly FirebaseService _firebase;

        // ── Pagination constants ──────────────────────────────────────────────
        private const int ToolPageSize = 10;
        private const int WorkerPageSize = 10;

        // ── Raw full lists (built once per load, sliced for display) ─────────
        private List<Tool> _allBorrowedTools = new();
        private List<WorkerActivityItem> _allWorkerActivities = new();

        // ── Pagination cursors ────────────────────────────────────────────────
        private int _borrowedToolPage = 0;
        private int _workerPage = 0;

        // ── Identity ──────────────────────────────────────────────────────────
        public string EngineerName =>
            _auth.CurrentUser?.FullName ?? "Project Engineer";
        public string EngineerInitials =>
            GetInitials(_auth.CurrentUser?.FullName);
        public string GreetingText => GetGreeting();
        public string TodayDate =>
            DateTime.Now.ToString("dddd, MMMM d, yyyy");

        // ── Theme ─────────────────────────────────────────────────────────────
        public string ThemeIcon => _theme.IsDark ? "🌙" : "☀️";

        // ── Stats ─────────────────────────────────────────────────────────────
        private int _totalTools;
        public int TotalTools
        {
            get => _totalTools;
            private set => SetProperty(ref _totalTools, value);
        }

        private int _availableTools;
        public int AvailableTools
        {
            get => _availableTools;
            private set => SetProperty(ref _availableTools, value);
        }

        private int _borrowedTools;
        public int BorrowedTools
        {
            get => _borrowedTools;
            private set => SetProperty(ref _borrowedTools, value);
        }

        private int _damagedTools;
        public int DamagedTools
        {
            get => _damagedTools;
            private set
            {
                SetProperty(ref _damagedTools, value);
                OnPropertyChanged(nameof(HasDamagedTools));
            }
        }

        private int _totalWorkers;
        public int TotalWorkers
        {
            get => _totalWorkers;
            private set => SetProperty(ref _totalWorkers, value);
        }

        private int _pendingReports;
        public int PendingReports
        {
            get => _pendingReports;
            private set
            {
                SetProperty(ref _pendingReports, value);
                OnPropertyChanged(nameof(HasPendingReports));
            }
        }

        private int _pendingPauseCount;
        public int PendingPauseCount
        {
            get => _pendingPauseCount;
            private set => SetProperty(ref _pendingPauseCount, value);
        }

        public bool HasDamagedTools => DamagedTools > 0;
        public bool HasPendingReports => PendingReports > 0;
        public bool HasPendingPause => PendingPauseCount > 0;

        // ── Paged collections ─────────────────────────────────────────────────
        public ObservableCollection<Tool> RecentTools { get; } = new();

        private bool _hasMoreBorrowedTools;
        public bool HasMoreBorrowedTools
        {
            get => _hasMoreBorrowedTools;
            private set => SetProperty(ref _hasMoreBorrowedTools, value);
        }

        private string _borrowedToolsLabel = string.Empty;
        public string BorrowedToolsLabel
        {
            get => _borrowedToolsLabel;
            private set => SetProperty(ref _borrowedToolsLabel, value);
        }

        public ObservableCollection<WorkerActivityItem> WorkerActivities { get; } = new();

        private bool _hasMoreWorkers;
        public bool HasMoreWorkers
        {
            get => _hasMoreWorkers;
            private set => SetProperty(ref _hasMoreWorkers, value);
        }

        private string _workerActivityLabel = string.Empty;
        public string WorkerActivityLabel
        {
            get => _workerActivityLabel;
            private set => SetProperty(ref _workerActivityLabel, value);
        }

        // ── Damage reports mini-list (max 5, no pagination needed) ────────────
        public ObservableCollection<DamageReport> PendingDamageReports { get; } = new();

        // ── Empty-state helpers ───────────────────────────────────────────────
        public bool NoBorrowedTools => RecentTools.Count == 0 && !IsBusy;
        public bool NoWorkerActivity => WorkerActivities.Count == 0 && !IsBusy;

        // ── Pull-to-refresh ───────────────────────────────────────────────────
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewAllToolsCommand { get; }
        public ICommand ViewDamageReportsCommand { get; }
        public ICommand ViewWorkersCommand { get; }
        public ICommand ApproveDamageCommand { get; }
        public ICommand ViewAllTransactionsCommand { get; }
        public ICommand ViewProjectsCommand { get; }
        public ICommand ViewPauseRequestsCommand { get; }
        public ICommand LoadMoreBorrowedCommand { get; }
        public ICommand LoadMoreWorkersCommand { get; }
        public ICommand ScanQrCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────
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
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            ToggleThemeCommand = new Command(() => _theme.Toggle());
            LogoutCommand = new Command(async () => await LogoutAsync());
            RefreshCommand = new Command(async () => await RefreshAsync());
            ScanQrCommand = new Command(async () => await ScanQrAsync());

            ViewAllToolsCommand = new Command(async () =>
                await Shell.Current.GoToAsync("//EquipmentCatalogView"));
            ViewDamageReportsCommand = new Command(async () =>
                await Shell.Current.GoToAsync("//DamageReportsView"));
            ViewWorkersCommand = new Command(async () =>
                await Shell.Current.GoToAsync("//WorkerManagementView"));
            ViewProjectsCommand = new Command(async () =>
                await Shell.Current.GoToAsync("//ProjectManagementView"));
            ViewPauseRequestsCommand = new Command(async () =>
                await Shell.Current.GoToAsync("//PauseRequestsView"));
            ViewAllTransactionsCommand = new Command(async () =>
                await Shell.Current.GoToAsync(
                    $"{nameof(TransactionHistoryView)}?viewMode=all"));

            ApproveDamageCommand = new Command<DamageReport>(
                async report => await HandleDamageReportAsync(report));

            LoadMoreBorrowedCommand = new Command(
                execute: LoadNextBorrowedPage,
                canExecute: () => HasMoreBorrowedTools);

            LoadMoreWorkersCommand = new Command(
                execute: LoadNextWorkerPage,
                canExecute: () => HasMoreWorkers);
        }

        // ── Primary load ──────────────────────────────────────────────────────
        public async Task LoadAsync(bool forceRefresh = false)
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                // GetAllUsersAsync() has no forceRefresh overload — always fetches live.
                // The other three respect their FirebaseService in-memory caches.
                var toolsTask = _firebase.GetAllToolsAsync(forceRefresh);
                var usersTask = _firebase.GetAllUsersAsync();
                var pauseTask = _firebase.GetAllPauseRequestsRawAsync();
                var damageTask = _firebase.GetAllDamageReportsRawAsync();

                await Task.WhenAll(toolsTask, usersTask, pauseTask, damageTask);

                List<Tool> allTools = toolsTask.Result;   // List<Tool>
                List<User> allUsers = usersTask.Result;   // List<User>
                List<PauseRequestResult> pauseRequests = pauseTask.Result; // List<PauseRequestResult>
                List<DamageReportResult> rawReports = damageTask.Result;// List<DamageReportResult>

                // ── Stats ─────────────────────────────────────────────────────
                TotalTools = allTools.Count;
                AvailableTools = allTools.Count(t => t.Status == "Available");
                BorrowedTools = allTools.Count(t => t.Status == "Borrowed");
                DamagedTools = allTools.Count(t =>
                    t.Status == "Damaged" || t.Status == "UnderRepair");

                var approvedWorkers = allUsers
                    .Where(u => u.Role == "Worker" && u.AccountStatus == "Approved")
                    .ToList();

                var pendingWorkerCount = allUsers
                    .Count(u => u.Role == "Worker" && u.AccountStatus == "Pending");

                TotalWorkers = approvedWorkers.Count;

                // PauseRequestResult.Request → PauseRequest
                PendingPauseCount = pauseRequests
                    .Count(r => r.Request.Status == "Pending");

                // DamageReportResult.Report → DamageReport
                var pendingDamageCount = rawReports
                    .Count(r => r.Report.Status == "Pending");

                PendingReports =
                    pendingDamageCount + pendingWorkerCount + PendingPauseCount;

                // ── Damage reports mini-list (max 5) ──────────────────────────
                PendingDamageReports.Clear();
                foreach (var item in rawReports
                    .Where(r => r.Report.Status == "Pending")
                    .Take(5))
                    PendingDamageReports.Add(item.Report);

                // ── Borrowed tools — full sorted list → show page 1 ───────────
                _allBorrowedTools = allTools
                    .Where(t => t.Status == "Borrowed")
                    .OrderByDescending(t => t.BorrowDate)
                    .ToList();

                _borrowedToolPage = 0;
                RecentTools.Clear();
                AppendBorrowedPage();

                // ── Worker activities — full list → show page 1 ───────────────
                _allWorkerActivities = approvedWorkers
                    .Select(worker => new WorkerActivityItem
                    {
                        WorkerName = worker.FullName,
                        WorkerInitials = GetInitials(worker.FullName),
                        AssignedTools = allTools
                            .Count(t => t.AssignedWorkerId == worker.UniqueKey),
                        Status = allTools
                            .Any(t => t.AssignedWorkerId == worker.UniqueKey)
                            ? "Active"
                            : "Idle"
                    })
                    .OrderByDescending(w => w.AssignedTools)
                    .ToList();

                _workerPage = 0;
                WorkerActivities.Clear();
                AppendWorkerPage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PEDashboardVM] Load error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                NotifyEmptyStates();
            }
        }

        // ── Pagination: borrowed tools ────────────────────────────────────────
        private void AppendBorrowedPage()
        {
            var slice = _allBorrowedTools
                .Skip(_borrowedToolPage * ToolPageSize)
                .Take(ToolPageSize);

            foreach (var tool in slice)
                RecentTools.Add(tool);

            UpdateBorrowedPaginationState();
        }

        private void LoadNextBorrowedPage()
        {
            if (!HasMoreBorrowedTools) return;
            _borrowedToolPage++;
            AppendBorrowedPage();
        }

        private void UpdateBorrowedPaginationState()
        {
            int visible = RecentTools.Count;
            int total = _allBorrowedTools.Count;
            HasMoreBorrowedTools = visible < total;
            BorrowedToolsLabel = total == 0
                ? string.Empty
                : $"Showing {visible} of {total} borrowed tools";
            ((Command)LoadMoreBorrowedCommand).ChangeCanExecute();
        }

        // ── Pagination: worker activity ───────────────────────────────────────
        private void AppendWorkerPage()
        {
            var slice = _allWorkerActivities
                .Skip(_workerPage * WorkerPageSize)
                .Take(WorkerPageSize);

            foreach (var item in slice)
                WorkerActivities.Add(item);

            UpdateWorkerPaginationState();
        }

        private void LoadNextWorkerPage()
        {
            if (!HasMoreWorkers) return;
            _workerPage++;
            AppendWorkerPage();
        }

        private void UpdateWorkerPaginationState()
        {
            int visible = WorkerActivities.Count;
            int total = _allWorkerActivities.Count;
            HasMoreWorkers = visible < total;
            WorkerActivityLabel = total == 0
                ? string.Empty
                : $"Showing {visible} of {total} workers";
            ((Command)LoadMoreWorkersCommand).ChangeCanExecute();
        }

        // ── Refresh ───────────────────────────────────────────────────────────
        public async Task RefreshOnAppearingAsync() =>
            await LoadAsync(forceRefresh: false);

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadAsync(forceRefresh: true);
            IsRefreshing = false;
        }

        // ── Handle Damage Report ──────────────────────────────────────────────
        private async Task HandleDamageReportAsync(DamageReport report)
        {
            if (report is null) return;

            var action = await Shell.Current.DisplayActionSheet(
                $"Handle Report — {report.ToolName}",
                "Cancel", null,
                "✅ Mark as Resolved",
                "🔨 Send to Repair",
                "❌ Mark Tool as Lost");

            if (action == "Cancel" || string.IsNullOrEmpty(action)) return;

            try
            {
                var allReports = await _firebase.GetAllDamageReportsRawAsync();
                var match = allReports.FirstOrDefault(r =>
                    r.Report.ToolId == report.ToolId &&
                    r.Report.WorkerId == report.WorkerId);

                if (match == null) return;

                match.Report.Status = action switch
                {
                    "✅ Mark as Resolved" => "Resolved",
                    "🔨 Send to Repair" => "UnderRepair",
                    "❌ Mark Tool as Lost" => "Lost",
                    _ => match.Report.Status
                };

                await _firebase.UpdateDamageReportAsync(match.Key, match.Report);

                var tool = await _firebase.GetToolByIdAsync(report.ToolId);
                if (tool != null)
                {
                    tool.Status = action switch
                    {
                        "✅ Mark as Resolved" => "Available",
                        "🔨 Send to Repair" => "UnderRepair",
                        "❌ Mark Tool as Lost" => "Lost",
                        _ => tool.Status
                    };

                    if (tool.Status == "Available" || tool.Status == "Lost")
                    {
                        tool.AssignedWorkerId = string.Empty;
                        tool.AssignedWorkerName = string.Empty;
                        tool.BorrowDate = null;
                    }

                    await _firebase.UpdateToolAsync(tool);
                }

                await Shell.Current.DisplayAlert(
                    "✅ Updated",
                    $"Damage report for {report.ToolName} has been updated.",
                    "OK");

                await LoadAsync(forceRefresh: true);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error", $"Could not update report.\n{ex.Message}", "OK");
            }
        }

        // ── Navigation ────────────────────────────────────────────────────────
        private async Task ScanQrAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(QrScannerView));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task LogoutAsync()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Logout", "Are you sure you want to logout?",
                "Logout", "Cancel");
            if (!confirm) return;
            _auth.Logout();
            await Shell.Current.GoToAsync("//LoginView");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void NotifyEmptyStates()
        {
            OnPropertyChanged(nameof(NoBorrowedTools));
            OnPropertyChanged(nameof(NoWorkerActivity));
        }

        private static string GetInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "PE";
            var parts = name.Trim().Split(' ',
                StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                : name[0].ToString().ToUpper();
        }

        private static string GetGreeting()
        {
            var h = DateTime.Now.Hour;
            return h < 12 ? "Good morning"
                 : h < 17 ? "Good afternoon"
                 : "Good evening";
        }
    }
}