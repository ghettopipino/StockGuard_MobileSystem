using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    public class ProjectAnalyticsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly ThemeService _theme;

        // =========================================================
        // RAW DATA
        // =========================================================

        private List<Project> _allProjects = new();
        private List<Tool> _allTools = new();
        private List<User> _allUsers = new();
        private List<TransactionLog> _allTransactions = new();
        private List<DamageReport> _allReports = new();

        private List<ProjectEquipmentRequirement>
            _projectRequirements = new();


        // =========================================================
        // RISK INSIGHTS
        // =========================================================

        private List<ToolRiskItem> _highRiskTools = new();

        public List<ToolRiskItem> HighRiskTools
        {
            get => _highRiskTools;

            private set
            {
                _highRiskTools = value;
                OnPropertyChanged();
            }
        }


        private List<WorkerRiskItem>
            _frequentlyInvolvedWorkers = new();

        public List<WorkerRiskItem>
            FrequentlyInvolvedWorkers
        {
            get => _frequentlyInvolvedWorkers;

            private set
            {
                _frequentlyInvolvedWorkers = value;
                OnPropertyChanged();
            }
        }


        // =========================================================
        // THEME
        // =========================================================

        public string ThemeIcon =>
            _theme.IsDark
                ? "🌙"
                : "☀️";


        // =========================================================
        // PROJECT PICKER
        // =========================================================

        public ObservableCollection<Project>
            Projects
        { get; } = new();


        private Project? _selectedProject;

        public Project? SelectedProject
        {
            get => _selectedProject;

            set
            {
                if (SetProperty(
                        ref _selectedProject,
                        value))
                {
                    OnPropertyChanged(
                        nameof(HasSelectedProject));

                    OnPropertyChanged(
                        nameof(IsIdle));

                    if (value != null)
                    {
                        MainThread.BeginInvokeOnMainThread(
                            async () =>
                                await LoadSelectedProjectStatsAsync());
                    }
                    else
                    {
                        ClearStats();
                    }
                }
            }
        }


        public bool HasSelectedProject =>
            SelectedProject != null &&
            !IsBusy;


        public bool IsIdle =>
            !IsBusy &&
            SelectedProject == null;


        // =========================================================
        // TOOL STATS
        // =========================================================

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


        private int _damagedTools;

        public int DamagedTools
        {
            get => _damagedTools;

            private set =>
                SetProperty(
                    ref _damagedTools,
                    value);
        }


        private int _lostTools;

        public int LostTools
        {
            get => _lostTools;

            private set =>
                SetProperty(
                    ref _lostTools,
                    value);
        }


        // =========================================================
        // TRANSACTION STATS
        // =========================================================

        private int _totalTransactions;

        public int TotalTransactions
        {
            get => _totalTransactions;

            private set =>
                SetProperty(
                    ref _totalTransactions,
                    value);
        }


        private int _totalBorrows;

        public int TotalBorrows
        {
            get => _totalBorrows;

            private set =>
                SetProperty(
                    ref _totalBorrows,
                    value);
        }


        private int _totalReturns;

        public int TotalReturns
        {
            get => _totalReturns;

            private set =>
                SetProperty(
                    ref _totalReturns,
                    value);
        }


        private int _totalTransfers;

        public int TotalTransfers
        {
            get => _totalTransfers;

            private set =>
                SetProperty(
                    ref _totalTransfers,
                    value);
        }


        // =========================================================
        // WORKER PERFORMANCE
        // =========================================================

        public ObservableCollection<WorkerStatItem>
            WorkerStats
        { get; } = new();


        private WorkerStatItem? _mostActiveWorker;

        public WorkerStatItem? MostActiveWorker
        {
            get => _mostActiveWorker;

            private set
            {
                SetProperty(
                    ref _mostActiveWorker,
                    value);

                OnPropertyChanged(
                    nameof(HasMostActiveWorker));
            }
        }


        public bool HasMostActiveWorker =>
            MostActiveWorker != null &&
            MostActiveWorker.TotalActivity > 0;


        // =========================================================
        // TOOL USAGE
        // =========================================================

        public ObservableCollection<ToolStatItem>
            ToolStats
        { get; } = new();


        private ToolStatItem? _mostUsedTool;

        public ToolStatItem? MostUsedTool
        {
            get => _mostUsedTool;

            private set
            {
                SetProperty(
                    ref _mostUsedTool,
                    value);

                OnPropertyChanged(
                    nameof(HasMostUsedTool));
            }
        }


        public bool HasMostUsedTool =>
            MostUsedTool != null &&
            MostUsedTool.Usage > 0;


        // =========================================================
        // DAMAGE REPORT SUMMARY
        // =========================================================

        private int _totalReports;

        public int TotalReports
        {
            get => _totalReports;

            private set =>
                SetProperty(
                    ref _totalReports,
                    value);
        }


        private int _pendingReports;

        public int PendingReports
        {
            get => _pendingReports;

            private set =>
                SetProperty(
                    ref _pendingReports,
                    value);
        }


        private int _resolvedReports;

        public int ResolvedReports
        {
            get => _resolvedReports;

            private set =>
                SetProperty(
                    ref _resolvedReports,
                    value);
        }


        // =========================================================
        // DISPUTED REPORTS
        // =========================================================

        private int _disputedReports;

        public int DisputedReports
        {
            get => _disputedReports;

            private set =>
                SetProperty(
                    ref _disputedReports,
                    value);
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get => _isRefreshing;

            set =>
                SetProperty(
                    ref _isRefreshing,
                    value);
        }


        // =========================================================
        // COMMANDS
        // =========================================================

        public ICommand RefreshCommand { get; }

        public ICommand ToggleThemeCommand { get; }


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ProjectAnalyticsViewModel(
            FirebaseService firebase,
            ThemeService theme)
        {
            _firebase = firebase;
            _theme = theme;

            Title = "Analytics";


            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(
                    () =>
                        OnPropertyChanged(
                            nameof(ThemeIcon)));


            RefreshCommand =
                new Command(
                    async () =>
                        await RefreshAsync());


            ToggleThemeCommand =
                new Command(
                    () =>
                        _theme.Toggle());
        }


        // =========================================================
        // LOAD ALL DATA
        // =========================================================

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var previousProjectId =
                    SelectedProject?.ProjectId;


                // ─────────────────────────────────────────────
                // LOAD FIREBASE DATA
                // ─────────────────────────────────────────────

                var projectsTask =
                    _firebase.GetAllProjectsAsync();

                var toolsTask =
                    _firebase.GetAllToolsAsync(
                        forceRefresh: true);

                var usersTask =
                    _firebase.GetAllUsersAsync();

                var transactionsTask =
                    _firebase.GetAllTransactionsAsync();

                var reportsTask =
                    _firebase.GetAllDamageReportsAsync();


                await Task.WhenAll(
                    projectsTask,
                    toolsTask,
                    usersTask,
                    transactionsTask,
                    reportsTask);


                _allProjects =
                    projectsTask.Result ??
                    new List<Project>();

                _allTools =
                    toolsTask.Result ??
                    new List<Tool>();

                _allUsers =
                    usersTask.Result ??
                    new List<User>();

                _allTransactions =
                    transactionsTask.Result ??
                    new List<TransactionLog>();

                _allReports =
                    reportsTask.Result ??
                    new List<DamageReport>();


                // ─────────────────────────────────────────────
                // PROJECT PICKER
                // ─────────────────────────────────────────────

                Projects.Clear();


                foreach (var project in
                    _allProjects
                        .Where(p =>
                            !p.IsDeleted)
                        .OrderByDescending(p =>
                            p.StartDate))
                {
                    Projects.Add(project);
                }


                Project? projectToSelect = null;


                if (!string.IsNullOrWhiteSpace(
                        previousProjectId))
                {
                    projectToSelect =
                        Projects.FirstOrDefault(p =>
                            Same(
                                p.ProjectId,
                                previousProjectId));
                }


                projectToSelect ??=
                    Projects.FirstOrDefault(p =>
                        Same(
                            p.Status,
                            "Completed"));


                projectToSelect ??=
                    Projects.FirstOrDefault();


                _selectedProject =
                    projectToSelect;

                OnPropertyChanged(
                    nameof(SelectedProject));


                // ─────────────────────────────────────────────
                // PROJECT REQUIREMENTS
                // ─────────────────────────────────────────────

                if (_selectedProject != null)
                {
                    _projectRequirements =
                        await _firebase
                            .GetProjectEquipmentRequirementsAsync(
                                _selectedProject.ProjectId)
                        ?? new List<ProjectEquipmentRequirement>();

                    ComputeStats();
                }
                else
                {
                    _projectRequirements.Clear();

                    ClearStats();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AnalyticsVM] Load error: " +
                    $"{ex.Message}");
            }
            finally
            {
                IsBusy = false;

                OnPropertyChanged(
                    nameof(HasSelectedProject));

                OnPropertyChanged(
                    nameof(IsIdle));
            }
        }


        // =========================================================
        // LOAD SELECTED PROJECT
        // =========================================================

        private async Task LoadSelectedProjectStatsAsync()
        {
            if (SelectedProject == null)
            {
                ClearStats();
                return;
            }

            try
            {
                _allTools =
                    await _firebase
                        .GetAllToolsAsync(
                            forceRefresh: true)
                    ?? new List<Tool>();


                _allReports =
                    await _firebase
                        .GetAllDamageReportsAsync()
                    ?? new List<DamageReport>();


                _allTransactions =
                    await _firebase
                        .GetAllTransactionsAsync()
                    ?? new List<TransactionLog>();


                _projectRequirements =
                    await _firebase
                        .GetProjectEquipmentRequirementsAsync(
                            SelectedProject.ProjectId)
                    ?? new List<ProjectEquipmentRequirement>();


                ComputeStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AnalyticsVM] Selected project error: " +
                    $"{ex.Message}");
            }
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async Task RefreshAsync()
        {
            if (IsRefreshing)
                return;

            IsRefreshing = true;

            try
            {
                await LoadAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }


        // =========================================================
        // COMPUTE ANALYTICS
        // =========================================================

        private void ComputeStats()
        {
            if (SelectedProject == null)
            {
                ClearStats();
                return;
            }


            string projectId =
                SelectedProject.ProjectId;


            // =====================================================
            // PROJECT DAMAGE REPORTS
            // =====================================================

            var projectReports =
                _allReports
                    .Where(report =>
                        Same(
                            report.ProjectId,
                            projectId))
                    .ToList();


            // =====================================================
            // TOOL OVERVIEW
            // =====================================================

            TotalTools =
                _projectRequirements
                    .Sum(requirement =>
                        requirement.QuantityNeeded);


            // =====================================================
            // DISTRIBUTED / ACTIVE EQUIPMENT
            // =====================================================

            int distributedCount =
                _allTools.Count(tool =>
                    !tool.IsDeleted &&
                    Same(
                        tool.BorrowedProjectId,
                        projectId) &&
                    (
                        Same(
                            tool.Status,
                            "Borrowed") ||
                        Same(
                            tool.Status,
                            "PendingReturn")
                    ));


            // =====================================================
            // AVAILABLE EQUIPMENT
            // =====================================================

            AvailableTools =
                Math.Max(
                    0,
                    TotalTools -
                    distributedCount);


            // =====================================================
            // DAMAGED EQUIPMENT
            // =====================================================

            DamagedTools =
                _allTools.Count(tool =>
                    !tool.IsDeleted &&
                    Same(
                        tool.BorrowedProjectId,
                        projectId) &&
                    (
                        Same(
                            tool.Status,
                            "Damaged") ||
                        Same(
                            tool.Status,
                            "UnderRepair")
                    ));


            // =====================================================
            // LOST EQUIPMENT
            // =====================================================

            LostTools =
                projectReports
                    .Where(report =>
                        Same(
                            report.Status,
                            "Lost"))
                    .Where(report =>
                        !string.IsNullOrWhiteSpace(
                            report.ToolId))
                    .Select(report =>
                        report.ToolId)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .Count();


            // =====================================================
            // PROJECT TRANSACTIONS
            // =====================================================

            var projectTransactions =
                _allTransactions
                    .Where(transaction =>
                        Same(
                            transaction.ProjectId,
                            projectId))
                    .ToList();


            TotalTransactions =
                projectTransactions.Count;


            TotalBorrows =
                projectTransactions.Count(transaction =>
                    Same(
                        transaction.Action,
                        "Borrowed"));


            TotalReturns =
                projectTransactions.Count(transaction =>
                    Same(
                        transaction.Action,
                        "Returned") ||
                    Same(
                        transaction.Action,
                        "Returned Damaged"));


            TotalTransfers =
                projectTransactions.Count(transaction =>
                    Same(
                        transaction.Action,
                        "Transferred"));


            // =====================================================
            // DAMAGE REPORT SUMMARY
            // =====================================================

            TotalReports =
                projectReports.Count;


            PendingReports =
                projectReports.Count(report =>
                    Same(
                        report.Status,
                        "Pending") ||
                    Same(
                        report.Status,
                        "UnderRepair"));


            ResolvedReports =
                projectReports.Count(report =>
                    Same(
                        report.Status,
                        "Resolved") ||
                    Same(
                        report.Status,
                        "Lost"));


            DisputedReports =
                projectReports.Count(report =>
                    Same(
                        report.Status,
                        "Disputed"));


            // =====================================================
            // PROJECT WORKERS
            // =====================================================

            var workerIds =
                projectTransactions
                    .Where(transaction =>
                        !string.IsNullOrWhiteSpace(
                            transaction.WorkerId))
                    .Select(transaction =>
                        transaction.WorkerId)

                    .Concat(
                        projectReports
                            .Where(report =>
                                !string.IsNullOrWhiteSpace(
                                    report.WorkerId))
                            .Select(report =>
                                report.WorkerId))

                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)

                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);


            var projectWorkers =
                _allUsers
                    .Where(user =>
                        Same(
                            user.Role,
                            "Worker") &&
                        Same(
                            user.AccountStatus,
                            "Approved") &&
                        workerIds.Contains(
                            user.UniqueKey))
                    .ToList();


            // =====================================================
            // WORKER PERFORMANCE
            // =====================================================
            //
            // Borrows:
            // Equipment originally borrowed/accepted by worker.
            //
            // TransfersReceived:
            // Equipment received by worker through a transfer.
            //
            // IMPORTANT:
            // A transfer does NOT increase Borrows.
            // It is recorded separately as TransfersReceived.
            // =====================================================

            var workerStats =
                projectWorkers
                    .Select(worker =>
                        new WorkerStatItem
                        {
                            WorkerId =
                                worker.UniqueKey,

                            WorkerName =
                                worker.FullName,

                            Borrows =
                                projectTransactions.Count(transaction =>
                                    Same(
                                        transaction.WorkerId,
                                        worker.UniqueKey) &&
                                    Same(
                                        transaction.Action,
                                        "Borrowed")),

                            TransfersReceived =
                                projectTransactions.Count(transaction =>
                                    Same(
                                        transaction.WorkerId,
                                        worker.UniqueKey) &&
                                    Same(
                                        transaction.Action,
                                        "Transferred")),

                            Damages =
                                projectReports.Count(report =>
                                    Same(
                                        report.WorkerId,
                                        worker.UniqueKey))
                        })

                    .OrderByDescending(worker =>
                        worker.TotalActivity)

                    .ThenBy(worker =>
                        worker.WorkerName)

                    .ToList();


            WorkerStats.Clear();


            foreach (var worker in workerStats)
            {
                WorkerStats.Add(worker);
            }


            MostActiveWorker =
                workerStats
                    .OrderByDescending(worker =>
                        worker.TotalActivity)
                    .ThenBy(worker =>
                        worker.WorkerName)
                    .FirstOrDefault();


            // =====================================================
            // PROJECT TOOL IDS
            // =====================================================

            var projectToolIds =
                projectTransactions
                    .Where(transaction =>
                        !string.IsNullOrWhiteSpace(
                            transaction.ToolId))
                    .Select(transaction =>
                        transaction.ToolId)

                    .Concat(
                        projectReports
                            .Where(report =>
                                !string.IsNullOrWhiteSpace(
                                    report.ToolId))
                            .Select(report =>
                                report.ToolId))

                    .Concat(
                        _allTools
                            .Where(tool =>
                                Same(
                                    tool.BorrowedProjectId,
                                    projectId))
                            .Select(tool =>
                                tool.ToolId))

                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)

                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);


            // =====================================================
            // TOOL USAGE
            // =====================================================

            var toolStats =
                _allTools
                    .Where(tool =>
                        !tool.IsDeleted &&
                        projectToolIds.Contains(
                            tool.ToolId))

                    .Select(tool =>
                        new ToolStatItem
                        {
                            ToolId =
                                tool.ToolId,

                            ToolName =
                                tool.ToolName,

                            Status =
                                tool.Status,

                            Usage =
                                projectTransactions.Count(transaction =>
                                    Same(
                                        transaction.ToolId,
                                        tool.ToolId) &&
                                    Same(
                                        transaction.Action,
                                        "Borrowed")),

                            Damages =
                                projectReports.Count(report =>
                                    Same(
                                        report.ToolId,
                                        tool.ToolId))
                        })

                    .OrderByDescending(tool =>
                        tool.Usage)

                    .ThenBy(tool =>
                        tool.ToolName)

                    .ToList();


            ToolStats.Clear();


            foreach (var tool in toolStats)
            {
                ToolStats.Add(tool);
            }


            MostUsedTool =
                toolStats
                    .OrderByDescending(tool =>
                        tool.Usage)
                    .FirstOrDefault();


            // =====================================================
            // HIGH-RISK TOOLS
            // =====================================================

            HighRiskTools =
                projectReports
                    .Where(report =>
                        !string.IsNullOrWhiteSpace(
                            report.ToolId))

                    .GroupBy(
                        report =>
                            report.ToolId,
                        StringComparer.OrdinalIgnoreCase)

                    .Where(group =>
                        group.Count() >= 2)

                    .Select(group =>
                    {
                        var firstReport =
                            group.First();

                        var physicalTool =
                            _allTools.FirstOrDefault(tool =>
                                Same(
                                    tool.ToolId,
                                    group.Key));

                        return new ToolRiskItem
                        {
                            ToolId =
                                group.Key,

                            ToolName =
                                physicalTool?.ToolName ??
                                firstReport.ToolName,

                            IncidentCount =
                                group.Count()
                        };
                    })

                    .OrderByDescending(tool =>
                        tool.IncidentCount)

                    .ToList();


            // =====================================================
            // FREQUENTLY INVOLVED WORKERS
            // =====================================================

            FrequentlyInvolvedWorkers =
                projectReports
                    .Where(report =>
                        !string.IsNullOrWhiteSpace(
                            report.WorkerId))

                    .GroupBy(
                        report =>
                            report.WorkerId,
                        StringComparer.OrdinalIgnoreCase)

                    .Where(group =>
                        group.Count() >= 2)

                    .Select(group =>
                    {
                        var firstReport =
                            group.First();

                        var user =
                            _allUsers.FirstOrDefault(user =>
                                Same(
                                    user.UniqueKey,
                                    group.Key));

                        return new WorkerRiskItem
                        {
                            WorkerId =
                                group.Key,

                            WorkerName =
                                user?.FullName ??
                                firstReport.WorkerName,

                            IncidentCount =
                                group.Count()
                        };
                    })

                    .OrderByDescending(worker =>
                        worker.IncidentCount)

                    .ToList();


            // =====================================================
            // NOTIFY UI
            // =====================================================

            OnPropertyChanged(
                nameof(HasSelectedProject));

            OnPropertyChanged(
                nameof(IsIdle));

            OnPropertyChanged(
                nameof(HasMostActiveWorker));

            OnPropertyChanged(
                nameof(HasMostUsedTool));
        }


        // =========================================================
        // CLEAR STATS
        // =========================================================

        private void ClearStats()
        {
            TotalTools = 0;
            AvailableTools = 0;
            DamagedTools = 0;
            LostTools = 0;

            TotalTransactions = 0;
            TotalBorrows = 0;
            TotalReturns = 0;
            TotalTransfers = 0;

            TotalReports = 0;
            PendingReports = 0;
            ResolvedReports = 0;
            DisputedReports = 0;

            WorkerStats.Clear();
            ToolStats.Clear();

            MostActiveWorker = null;
            MostUsedTool = null;

            HighRiskTools =
                new List<ToolRiskItem>();

            FrequentlyInvolvedWorkers =
                new List<WorkerRiskItem>();

            OnPropertyChanged(
                nameof(HasSelectedProject));

            OnPropertyChanged(
                nameof(IsIdle));
        }


        // =========================================================
        // STRING COMPARISON HELPER
        // =========================================================

        private static bool Same(
            string? first,
            string? second)
        {
            return string.Equals(
                first?.Trim(),
                second?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}