using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    /// <summary>
    /// Mirrors the web AnalyticsController.Index() logic exactly:
    ///   1. Load all projects, tools, users, transactions, damage reports
    ///   2. Let the user pick a project via SelectedProject
    ///   3. Compute tool stats, transaction stats, worker stats,
    ///      tool usage stats, and damage report summary
    ///
    /// Bindings consumed by ProjectAnalyticsView.xaml:
    ///   Projects, SelectedProject, HasSelectedProject, IsIdle
    ///   TotalTools, AvailableTools, DamagedTools, LostTools
    ///   TotalTransactions, TotalBorrows, TotalReturns, TotalTransfers
    ///   WorkerStats, MostActiveWorker, HasMostActiveWorker
    ///   ToolStats, MostUsedTool, HasMostUsedTool
    ///   TotalReports, PendingReports, ResolvedReports
    ///   IsRefreshing, IsBusy, ThemeIcon
    ///   RefreshCommand, ToggleThemeCommand, OpenFlyoutCommand (BaseViewModel)
    /// </summary>
    public class ProjectAnalyticsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly ThemeService _theme;

        // ── Raw data ──────────────────────────────────────────────────────────
        private List<Project> _allProjects = new();
        private List<Tool> _allTools = new();
        private List<User> _allUsers = new();
        private List<TransactionLog> _allTransactions = new();
        private List<DamageReport> _allReports = new();
        private List<ToolRiskItem> _highRiskTools = new();
        public List<ToolRiskItem> HighRiskTools
        {
            get => _highRiskTools;
            private set { _highRiskTools = value; OnPropertyChanged(); }
        }
        private List<WorkerRiskItem> _frequentlyInvolvedWorkers = new();
        public List<WorkerRiskItem> FrequentlyInvolvedWorkers
        {
            get => _frequentlyInvolvedWorkers;
            private set { _frequentlyInvolvedWorkers = value; OnPropertyChanged(); }
        }
        

        // ── Theme ─────────────────────────────────────────────────────────────
        public string ThemeIcon => _theme.IsDark ? "🌙" : "☀️";

        // ── Project picker ────────────────────────────────────────────────────
        public ObservableCollection<Project> Projects { get; } = new();

        private Project? _selectedProject;
        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                SetProperty(ref _selectedProject, value);
                OnPropertyChanged(nameof(HasSelectedProject));
                OnPropertyChanged(nameof(IsIdle));

                // Recompute all stats whenever the project changes —
                // mirrors web's form onchange="this.form.submit()"
                if (value != null)
                    ComputeStats();
            }
        }

        /// <summary>True once the user has picked a project. Drives the analytics content visibility.</summary>
        public bool HasSelectedProject => SelectedProject != null && !IsBusy;

        /// <summary>True when nothing is loading and no project is selected yet. Drives the empty state.</summary>
        public bool IsIdle => !IsBusy && SelectedProject == null;

        // ── Tool stats ────────────────────────────────────────────────────────
        // Web: ViewBag.TotalTools / AvailableTools / DamagedTools / LostTools
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

        private int _damagedTools;
        public int DamagedTools
        {
            get => _damagedTools;
            private set => SetProperty(ref _damagedTools, value);
        }

        private int _lostTools;
        public int LostTools
        {
            get => _lostTools;
            private set => SetProperty(ref _lostTools, value);
        }

        // ── Transaction stats ─────────────────────────────────────────────────
        // Web: ViewBag.TotalTransactions / TotalBorrows / TotalReturns / TotalTransfers
        private int _totalTransactions;
        public int TotalTransactions
        {
            get => _totalTransactions;
            private set => SetProperty(ref _totalTransactions, value);
        }

        private int _totalBorrows;
        public int TotalBorrows
        {
            get => _totalBorrows;
            private set => SetProperty(ref _totalBorrows, value);
        }

        private int _totalReturns;
        public int TotalReturns
        {
            get => _totalReturns;
            private set => SetProperty(ref _totalReturns, value);
        }

        private int _totalTransfers;
        public int TotalTransfers
        {
            get => _totalTransfers;
            private set => SetProperty(ref _totalTransfers, value);
        }

        // ── Worker performance ────────────────────────────────────────────────
        // Web: ViewBag.WorkerStats / MostActiveWorker
        public ObservableCollection<WorkerStatItem> WorkerStats { get; } = new();

        private WorkerStatItem? _mostActiveWorker;
        public WorkerStatItem? MostActiveWorker
        {
            get => _mostActiveWorker;
            private set
            {
                SetProperty(ref _mostActiveWorker, value);
                OnPropertyChanged(nameof(HasMostActiveWorker));
            }
        }
        public bool HasMostActiveWorker =>
            MostActiveWorker != null && MostActiveWorker.Borrows > 0;
            
        // ── Tool usage ────────────────────────────────────────────────────────
        // Web: ViewBag.ToolUsage / MostUsedTool
        public ObservableCollection<ToolStatItem> ToolStats { get; } = new();

        private ToolStatItem? _mostUsedTool;
        public ToolStatItem? MostUsedTool
        {
            get => _mostUsedTool;
            private set
            {
                SetProperty(ref _mostUsedTool, value);
                OnPropertyChanged(nameof(HasMostUsedTool));
            }
        }
        public bool HasMostUsedTool =>
            MostUsedTool != null && MostUsedTool.Usage > 0;

        // ── Damage report summary ─────────────────────────────────────────────
        // Web: ViewBag.TotalReports / PendingReports / ResolvedReports
        private int _totalReports;
        public int TotalReports
        {
            get => _totalReports;
            private set => SetProperty(ref _totalReports, value);
        }

        private int _pendingReports;
        public int PendingReports
        {
            get => _pendingReports;
            private set => SetProperty(ref _pendingReports, value);
        }

        private int _resolvedReports;
        public int ResolvedReports
        {
            get => _resolvedReports;
            private set => SetProperty(ref _resolvedReports, value);
        }

        // ── Pull-to-refresh ───────────────────────────────────────────────────
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public ProjectAnalyticsViewModel(
            FirebaseService firebase,
            ThemeService theme)
        {
            _firebase = firebase;
            _theme = theme;
            Title = "Analytics";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            RefreshCommand = new Command(async () => await RefreshAsync());
            ToggleThemeCommand = new Command(() => _theme.Toggle());
        }

        // ── Load ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Loads all raw data then selects the default project —
        /// mirrors controller: first Completed project, or first project overall.
        /// Called from OnAppearing in the code-behind.
        /// </summary>
        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                // Load all data in parallel for speed
                var projectsTask = _firebase.GetAllProjectsAsync();
                var toolsTask = _firebase.GetAllToolsAsync();
                var usersTask = _firebase.GetAllUsersAsync();
                var transactionsTask = _firebase.GetAllTransactionsAsync();
                var reportsTask = _firebase.GetAllDamageReportsAsync();

                await Task.WhenAll(
                    projectsTask, toolsTask, usersTask,
                    transactionsTask, reportsTask);

                _allProjects = projectsTask.Result ?? new();
                _allTools = toolsTask.Result ?? new();
                _allUsers = usersTask.Result ?? new();
                _allTransactions = transactionsTask.Result ?? new();
                _allReports = reportsTask.Result ?? new();

                // Rebuild project picker
                Projects.Clear();
                foreach (var p in _allProjects
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.StartDate))
                    Projects.Add(p);

                // Default selection mirrors controller:
                // first Completed project, or first project
                var defaultProject =
                    Projects.FirstOrDefault(p => p.Status == "Completed")
                    ?? Projects.FirstOrDefault();

                // Setting SelectedProject triggers ComputeStats()
                SelectedProject = defaultProject;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AnalyticsVM] Load error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasSelectedProject));
                OnPropertyChanged(nameof(IsIdle));
            }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadAsync();
            IsRefreshing = false;
        }

        // ── Compute stats ─────────────────────────────────────────────────────
        /// <summary>
        /// Mirrors the controller's stat computation exactly.
        /// Called whenever SelectedProject changes.
        ///
        /// NOTE: The web controller computes stats across ALL tools/transactions
        /// regardless of which project is selected (the project is used only for
        /// the banner display, not as a filter). This ViewModel does the same.
        /// If per-project filtering is desired in the future, filter _allTools
        /// and _allTransactions by project here.
        /// </summary>
        private void ComputeStats()
        {
            // ── Tool stats ────────────────────────────────────────────────────
            TotalTools = _allTools.Count;
            AvailableTools = _allTools.Count(t => t.Status == "Available");
            DamagedTools = _allTools.Count(t =>
                t.Status == "Damaged" || t.Status == "UnderRepair");
            LostTools = _allTools.Count(t => t.Status == "Lost");

            // ── Transaction stats ─────────────────────────────────────────────
            TotalTransactions = _allTransactions.Count;
            TotalBorrows = _allTransactions.Count(t => t.Action == "Borrowed");
            TotalReturns = _allTransactions.Count(t => t.Action == "Returned");
            TotalTransfers = _allTransactions.Count(t => t.Action == "Transferred");

            // ── Worker performance ────────────────────────────────────────────
            // Mirrors: workers.Select(w => { borrows, damages }).OrderByDesc(borrows)
            var approvedWorkers = _allUsers
                .Where(u => u.Role == "Worker" &&
                            u.AccountStatus == "Approved")
                .ToList();

            var workerStats = approvedWorkers
                .Select(w => new WorkerStatItem
                {
                    WorkerId = w.UniqueKey,
                    WorkerName = w.FullName,
                    Borrows = _allTransactions.Count(t =>
                                     t.WorkerId == w.UniqueKey &&
                                     t.Action == "Borrowed"),
                    Damages = _allReports.Count(r =>
                                     r.WorkerId == w.UniqueKey)
                })
                .OrderByDescending(w => w.Borrows)
                .ToList();

            WorkerStats.Clear();
            foreach (var w in workerStats)
                WorkerStats.Add(w);

            MostActiveWorker = workerStats.FirstOrDefault();

            // ── Tool usage ────────────────────────────────────────────────────
            // Mirrors: allTools.Select(t => { tool, usageCount, damageCount }).OrderByDesc(usage)
            var toolStats = _allTools
                .Select(t => new ToolStatItem
                {
                    ToolId = t.ToolId,
                    ToolName = t.ToolName,
                    Status = t.Status,
                    Usage = _allTransactions.Count(tx =>
                                   tx.ToolId == t.ToolId &&
                                   tx.Action == "Borrowed"),
                    Damages = _allReports.Count(r =>
                                   r.ToolId == t.ToolId)
                })
                .OrderByDescending(t => t.Usage)
                .ToList();

            ToolStats.Clear();
            foreach (var t in toolStats)
                ToolStats.Add(t);

            MostUsedTool = toolStats.FirstOrDefault();

            // ── Damage report summary ─────────────────────────────────────────
            TotalReports = _allReports.Count;
            PendingReports = _allReports.Count(r => r.Status == "Pending");
            ResolvedReports = _allReports.Count(r => r.Status == "Resolved");

            // Notify the XAML that content is ready
            OnPropertyChanged(nameof(HasSelectedProject));
            OnPropertyChanged(nameof(IsIdle));
            // ── INSIGHT LAYER (new) ───────────────────────────────────

            // High-risk tools: 2+ damage reports
            HighRiskTools = _allTools
                .Where(t => _allReports.Count(r => r.ToolId == t.ToolId) >= 2)
                .Select(t => new ToolRiskItem
                {
                    ToolId = t.ToolId,
                    ToolName = t.ToolName,
                    IncidentCount = _allReports.Count(r => r.ToolId == t.ToolId)
                })
                .OrderByDescending(t => t.IncidentCount)
                .ToList();

            // Frequently involved workers: appears in 2+ reports
            FrequentlyInvolvedWorkers = _allUsers
                .Where(u => u.Role == "Worker" &&
                            _allReports.Count(r => r.WorkerId == u.UniqueKey) >= 2)
                .Select(u => new WorkerRiskItem
                {
                    WorkerId = u.UniqueKey,
                    WorkerName = u.FullName,
                    IncidentCount = _allReports.Count(r => r.WorkerId == u.UniqueKey)
                })
                .OrderByDescending(w => w.IncidentCount)
                .ToList();

            
        }
    }
}