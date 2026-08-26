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

        // Project equipment allocation / requirements
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
            MostActiveWorker.Borrows > 0;


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
                // Remember currently selected project
                // so refresh does not change the picker.
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


                // Keep current selection after refresh.
                if (!string.IsNullOrWhiteSpace(
                        previousProjectId))
                {
                    projectToSelect =
                        Projects.FirstOrDefault(p =>
                            Same(
                                p.ProjectId,
                                previousProjectId));
                }


                // Default project.
                projectToSelect ??=
                    Projects.FirstOrDefault(p =>
                        Same(
                            p.Status,
                            "Completed"));


                projectToSelect ??=
                    Projects.FirstOrDefault();


                // Directly assign backing field while loading.
                // We will load the selected project's
                // requirements below.
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
                                _selectedProject.ProjectId);

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
                // Get latest physical tool states.
                _allTools =
                    await _firebase
                        .GetAllToolsAsync(
                            forceRefresh: true)
                    ?? new List<Tool>();


                // Get allocation for THIS project.
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
            // TOOL OVERVIEW
            // =====================================================
            //
            // IMPORTANT:
            //
            // ProjectDetailsViewModel defines project equipment
            // total using:
            //
            // requirements.Sum(r => r.QuantityNeeded)
            //
            // Therefore Analytics MUST use the same source.
            //
            // Example:
            //
            // Project allocation = 10
            // Distributed       = 5
            //
            // Total Tools       = 10
            // Available         = 5
            //
            // =====================================================


            TotalTools =
                _projectRequirements
                    .Sum(r =>
                        r.QuantityNeeded);


            // Tools currently under worker responsibility
            // for this project.
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


            AvailableTools =
                Math.Max(
                    0,
                    TotalTools -
                    distributedCount);


            // Damage / repair tools tied to this project.
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


            LostTools =
                _allTools.Count(tool =>
                    !tool.IsDeleted &&
                    Same(
                        tool.BorrowedProjectId,
                        projectId) &&
                    Same(
                        tool.Status,
                        "Lost"));


            // =====================================================
            // PROJECT TRANSACTIONS
            // =====================================================

            var projectTransactions =
                _allTransactions
                    .Where(t =>
                        Same(
                            t.ProjectId,
                            projectId))
                    .ToList();


            TotalTransactions =
                projectTransactions.Count;


            TotalBorrows =
                projectTransactions.Count(t =>
                    Same(
                        t.Action,
                        "Borrowed"));


            TotalReturns =
                projectTransactions.Count(t =>
                    Same(
                        t.Action,
                        "Returned") ||
                    Same(
                        t.Action,
                        "Returned Damaged"));


            TotalTransfers =
                projectTransactions.Count(t =>
                    Same(
                        t.Action,
                        "Transferred"));


            // =====================================================
            // PROJECT DAMAGE REPORTS
            // =====================================================

            var projectReports =
                _allReports
                    .Where(r =>
                        Same(
                            r.ProjectId,
                            projectId))
                    .ToList();


            TotalReports =
                projectReports.Count;


            PendingReports =
                projectReports.Count(r =>
                    Same(
                        r.Status,
                        "Pending") ||
                    Same(
                        r.Status,
                        "UnderRepair"));


            ResolvedReports =
                projectReports.Count(r =>
                    Same(
                        r.Status,
                        "Resolved") ||
                    Same(
                        r.Status,
                        "Lost"));


            // Keep this safe even if "Disputed"
            // is not currently used in every report.
            DisputedReports =
                projectReports.Count(r =>
                    Same(
                        r.Status,
                        "Disputed"));


            // =====================================================
            // PROJECT WORKERS
            // =====================================================

            var workerIds =
                projectTransactions
                    .Where(t =>
                        !string.IsNullOrWhiteSpace(
                            t.WorkerId))
                    .Select(t =>
                        t.WorkerId)
                    .Concat(
                        projectReports
                            .Where(r =>
                                !string.IsNullOrWhiteSpace(
                                    r.WorkerId))
                            .Select(r =>
                                r.WorkerId))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);


            var projectWorkers =
                _allUsers
                    .Where(u =>
                        Same(
                            u.Role,
                            "Worker") &&
                        Same(
                            u.AccountStatus,
                            "Approved") &&
                        workerIds.Contains(
                            u.UniqueKey))
                    .ToList();


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
                                projectTransactions.Count(t =>
                                    Same(
                                        t.WorkerId,
                                        worker.UniqueKey) &&
                                    Same(
                                        t.Action,
                                        "Borrowed")),

                            Damages =
                                projectReports.Count(r =>
                                    Same(
                                        r.WorkerId,
                                        worker.UniqueKey))
                        })
                    .OrderByDescending(w =>
                        w.Borrows)
                    .ThenBy(w =>
                        w.WorkerName)
                    .ToList();


            WorkerStats.Clear();


            foreach (var worker in workerStats)
            {
                WorkerStats.Add(worker);
            }


            MostActiveWorker =
                workerStats
                    .OrderByDescending(w =>
                        w.Borrows)
                    .FirstOrDefault();


            // =====================================================
            // TOOL USAGE
            // =====================================================
            //
            // Use tools that have appeared in THIS project's
            // transactions or reports.
            // =====================================================

            var projectToolIds =
                projectTransactions
                    .Where(t =>
                        !string.IsNullOrWhiteSpace(
                            t.ToolId))
                    .Select(t =>
                        t.ToolId)
                    .Concat(
                        projectReports
                            .Where(r =>
                                !string.IsNullOrWhiteSpace(
                                    r.ToolId))
                            .Select(r =>
                                r.ToolId))
                    .Concat(
                        _allTools
                            .Where(t =>
                                Same(
                                    t.BorrowedProjectId,
                                    projectId))
                            .Select(t =>
                                t.ToolId))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);


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
                                projectTransactions.Count(tx =>
                                    Same(
                                        tx.ToolId,
                                        tool.ToolId) &&
                                    Same(
                                        tx.Action,
                                        "Borrowed")),

                            Damages =
                                projectReports.Count(r =>
                                    Same(
                                        r.ToolId,
                                        tool.ToolId))
                        })
                    .OrderByDescending(t =>
                        t.Usage)
                    .ThenBy(t =>
                        t.ToolName)
                    .ToList();


            ToolStats.Clear();


            foreach (var tool in toolStats)
            {
                ToolStats.Add(tool);
            }


            MostUsedTool =
                toolStats
                    .OrderByDescending(t =>
                        t.Usage)
                    .FirstOrDefault();


            // =====================================================
            // HIGH-RISK TOOLS
            // =====================================================

            HighRiskTools =
                projectReports
                    .Where(r =>
                        !string.IsNullOrWhiteSpace(
                            r.ToolId))
                    .GroupBy(
                        r => r.ToolId,
                        StringComparer.OrdinalIgnoreCase)
                    .Where(g =>
                        g.Count() >= 2)
                    .Select(g =>
                    {
                        var firstReport =
                            g.First();

                        var physicalTool =
                            _allTools.FirstOrDefault(t =>
                                Same(
                                    t.ToolId,
                                    g.Key));

                        return new ToolRiskItem
                        {
                            ToolId =
                                g.Key,

                            ToolName =
                                physicalTool?.ToolName ??
                                firstReport.ToolName,

                            IncidentCount =
                                g.Count()
                        };
                    })
                    .OrderByDescending(t =>
                        t.IncidentCount)
                    .ToList();


            // =====================================================
            // FREQUENTLY INVOLVED WORKERS
            // =====================================================

            FrequentlyInvolvedWorkers =
                projectReports
                    .Where(r =>
                        !string.IsNullOrWhiteSpace(
                            r.WorkerId))
                    .GroupBy(
                        r => r.WorkerId,
                        StringComparer.OrdinalIgnoreCase)
                    .Where(g =>
                        g.Count() >= 2)
                    .Select(g =>
                    {
                        var firstReport =
                            g.First();

                        var user =
                            _allUsers.FirstOrDefault(u =>
                                Same(
                                    u.UniqueKey,
                                    g.Key));

                        return new WorkerRiskItem
                        {
                            WorkerId =
                                g.Key,

                            WorkerName =
                                user?.FullName ??
                                firstReport.WorkerName,

                            IncidentCount =
                                g.Count()
                        };
                    })
                    .OrderByDescending(w =>
                        w.IncidentCount)
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