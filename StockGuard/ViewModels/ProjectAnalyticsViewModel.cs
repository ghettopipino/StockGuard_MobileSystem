using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    public class ProjectAnalyticsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;


        // =========================================================
        // RAW DATA
        // =========================================================

        private List<Project> _allProjects =
            new();

        private List<Tool> _allTools =
            new();

        private List<User> _allUsers =
            new();

        private List<TransactionLog> _allTransactions =
            new();

        private List<DamageReport> _allDamageReports =
            new();

        private List<LostReport> _allLostReports =
            new();

        private List<ProjectEquipmentRequirement>
            _projectRequirements =
                new();

        private List<string> _projectWorkerIds =
            new();


        // =========================================================
        // RISK INSIGHTS
        // =========================================================

        private List<ToolRiskItem> _highRiskTools =
            new();

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
            _frequentlyInvolvedWorkers =
                new();

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
        // EQUIPMENT OVERVIEW
        // =========================================================

        private int _requiredTools;

        public int RequiredTools
        {
            get => _requiredTools;

            private set =>
                SetProperty(
                    ref _requiredTools,
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


        private int _remainingNeededTools;

        public int RemainingNeededTools
        {
            get => _remainingNeededTools;

            private set =>
                SetProperty(
                    ref _remainingNeededTools,
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
        // TRANSACTION SUMMARY
        // =========================================================

        private int _totalActivities;

        public int TotalActivities
        {
            get => _totalActivities;

            private set =>
                SetProperty(
                    ref _totalActivities,
                    value);
        }


        private int _projectBorrows;

        public int ProjectBorrows
        {
            get => _projectBorrows;

            private set =>
                SetProperty(
                    ref _projectBorrows,
                    value);
        }


        private int _workerReturns;

        public int WorkerReturns
        {
            get => _workerReturns;

            private set =>
                SetProperty(
                    ref _workerReturns,
                    value);
        }


        private int _totalCheckIns;

        public int TotalCheckIns
        {
            get => _totalCheckIns;

            private set =>
                SetProperty(
                    ref _totalCheckIns,
                    value);
        }


        // =========================================================
        // WORKER ACTIVITY
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


        private int _underRepairReports;

        public int UnderRepairReports
        {
            get => _underRepairReports;

            private set =>
                SetProperty(
                    ref _underRepairReports,
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

        public ICommand OpenFlyoutCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand ToggleThemeCommand { get; }


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ProjectAnalyticsViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;

            Title = "Analytics";


            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(
                    () =>
                        OnPropertyChanged(
                            nameof(ThemeIcon)));


            OpenFlyoutCommand =
                new Command(
                    () =>
                    {
                        if (Shell.Current != null)
                        {
                            Shell.Current.FlyoutIsPresented =
                                true;
                        }
                    });


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
        // LOAD
        // =========================================================

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
                    Projects.Clear();

                    _selectedProject = null;

                    OnPropertyChanged(
                        nameof(SelectedProject));

                    ClearStats();

                    return;
                }


                var previousProjectId =
                    SelectedProject?.ProjectId;


                // ─────────────────────────────────────────────
                // LOAD FIREBASE DATA
                // ─────────────────────────────────────────────

                var projectsTask =
                    _firebase
                        .GetAllProjectsAsync();

                var toolsTask =
                    _firebase
                        .GetAllToolsAsync(
                            forceRefresh);

                var usersTask =
                    _firebase
                        .GetAllUsersAsync();

                var transactionsTask =
                    _firebase
                        .GetAllTransactionsAsync(
                            forceRefresh);

                var damageReportsTask =
                    _firebase
                        .GetAllDamageReportsAsync();

                var lostReportsTask =
                    _firebase
                        .GetAllLostReportsAsync();


                await Task.WhenAll(
                    projectsTask,
                    toolsTask,
                    usersTask,
                    transactionsTask,
                    damageReportsTask,
                    lostReportsTask);


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

                _allDamageReports =
                    damageReportsTask.Result ??
                    new List<DamageReport>();

                _allLostReports =
                    lostReportsTask.Result ??
                    new List<LostReport>();


                // ─────────────────────────────────────────────
                // ONLY PROJECTS MANAGED BY CURRENT PE
                // ─────────────────────────────────────────────

                Projects.Clear();


                foreach (var project in
                    _allProjects
                        .Where(project =>
                            !project.IsDeleted &&
                            Same(
                                project.CreatedBy,
                                currentUser.UniqueKey))
                        .OrderByDescending(project =>
                            project.StartDate))
                {
                    Projects.Add(project);
                }


                Project? projectToSelect =
                    null;


                // Keep currently selected project.
                if (!string.IsNullOrWhiteSpace(
                        previousProjectId))
                {
                    projectToSelect =
                        Projects.FirstOrDefault(project =>
                            Same(
                                project.ProjectId,
                                previousProjectId));
                }


                // Prefer Active project.
                projectToSelect ??=
                    Projects.FirstOrDefault(project =>
                        Same(
                            project.Status,
                            "Active"));


                // Then Paused.
                projectToSelect ??=
                    Projects.FirstOrDefault(project =>
                        Same(
                            project.Status,
                            "Paused"));


                // Then any project.
                projectToSelect ??=
                    Projects.FirstOrDefault();


                _selectedProject =
                    projectToSelect;


                OnPropertyChanged(
                    nameof(SelectedProject));


                if (_selectedProject != null)
                {
                    await LoadProjectSpecificDataAsync(
                        _selectedProject.ProjectId);

                    ComputeStats();
                }
                else
                {
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

        private async Task
            LoadSelectedProjectStatsAsync()
        {
            if (SelectedProject == null)
            {
                ClearStats();

                return;
            }


            try
            {
                await LoadProjectSpecificDataAsync(
                    SelectedProject.ProjectId);

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
        // PROJECT-SPECIFIC DATA
        // =========================================================

        private async Task
            LoadProjectSpecificDataAsync(
                string projectId)
        {
            var requirementsTask =
                _firebase
                    .GetProjectEquipmentRequirementsAsync(
                        projectId);

            var workerIdsTask =
                _firebase
                    .GetProjectWorkerKeysAsync(
                        projectId);


            await Task.WhenAll(
                requirementsTask,
                workerIdsTask);


            _projectRequirements =
                requirementsTask.Result ??
                new List<ProjectEquipmentRequirement>();

            _projectWorkerIds =
                workerIdsTask.Result ??
                new List<string>();
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
                await LoadAsync(
                    forceRefresh: true);
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
            // PROJECT TRANSACTIONS
            // =====================================================

            var projectTransactions =
                _allTransactions
                    .Where(transaction =>
                        Same(
                            transaction.ProjectId,
                            projectId))
                    .ToList();


            // =====================================================
            // PROJECT DAMAGE REPORTS
            // =====================================================

            var projectDamageReports =
                _allDamageReports
                    .Where(report =>
                        Same(
                            report.ProjectId,
                            projectId))
                    .ToList();


            // =====================================================
            // PROJECT LOST / MISSING REPORTS
            // =====================================================

            var projectLostReports =
                _allLostReports
                    .Where(report =>
                        Same(
                            report.ProjectId,
                            projectId))
                    .ToList();


            // =====================================================
            // CURRENT PROJECT TOOLS
            // =====================================================

            var projectTools =
                _allTools
                    .Where(tool =>
                        !tool.IsDeleted &&
                        Same(
                            tool.BorrowedProjectId,
                            projectId))
                    .ToList();


            // =====================================================
            // EQUIPMENT OVERVIEW
            // =====================================================

            // Planned number of physical equipment needed.
            RequiredTools =
                _projectRequirements
                    .Sum(requirement =>
                        requirement.QuantityNeeded);


            // Physical equipment currently borrowed
            // into the selected project.
            BorrowedTools =
                projectTools.Count;


            // Equipment still needed to complete
            // the project's requirement.
            RemainingNeededTools =
                Math.Max(
                    0,
                    RequiredTools -
                    BorrowedTools);


            // Current damaged or under-repair
            // equipment still attached to project.
            DamagedTools =
                projectTools.Count(tool =>
                    Same(
                        tool.Status,
                        "Damaged") ||
                    Same(
                        tool.Status,
                        "UnderRepair"));


            // Only official Lost declarations count.
            LostTools =
                projectLostReports
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
            // TRANSACTION SUMMARY
            // =====================================================

            TotalActivities =
                projectTransactions.Count;


            // PE borrowed equipment from the office
            // into the project.
            ProjectBorrows =
                projectTransactions.Count(transaction =>
                    Same(
                        transaction.Action,
                        "Borrowed"));


            // Completed Worker return transactions.
            WorkerReturns =
                projectTransactions.Count(transaction =>
                    Same(
                        transaction.Action,
                        "Returned") ||
                    Same(
                        transaction.Action,
                        "Returned Damaged"));


            // Count one check-in submission as one check-in.
            // Verification is not counted as another check-in.
            TotalCheckIns =
                projectTransactions.Count(transaction =>
                    Same(
                        transaction.Action,
                        "End Day Check-In"));


            // =====================================================
            // DAMAGE REPORT SUMMARY
            // =====================================================

            TotalReports =
                projectDamageReports.Count;


            PendingReports =
                projectDamageReports.Count(report =>
                    Same(
                        report.Status,
                        "Pending"));


            UnderRepairReports =
                projectDamageReports.Count(report =>
                    Same(
                        report.Status,
                        "UnderRepair"));


            ResolvedReports =
                projectDamageReports.Count(report =>
                    Same(
                        report.Status,
                        "Resolved"));


            // =====================================================
            // PROJECT WORKERS
            // =====================================================

            var projectWorkers =
                _allUsers
                    .Where(user =>
                        Same(
                            user.Role,
                            "Worker") &&
                        Same(
                            user.AccountStatus,
                            "Approved") &&
                        _projectWorkerIds.Any(workerId =>
                            Same(
                                workerId,
                                user.UniqueKey)))
                    .ToList();


            // =====================================================
            // WORKER ACTIVITY
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


                            // Normal workflow:
                            // Worker accepts PE pre-assignment.
                            //
                            // Direct Borrowed with WorkerId is
                            // also accepted for compatibility.
                            AssignmentsAccepted =
                                projectTransactions.Count(
                                    transaction =>
                                        Same(
                                            transaction.WorkerId,
                                            worker.UniqueKey) &&
                                        (
                                            Same(
                                                transaction.Action,
                                                "Assignment Accepted") ||
                                            (
                                                Same(
                                                    transaction.Action,
                                                    "Borrowed") &&
                                                !string.IsNullOrWhiteSpace(
                                                    transaction.WorkerId)
                                            )
                                        )),


                            Returns =
                                projectTransactions.Count(
                                    transaction =>
                                        Same(
                                            transaction.WorkerId,
                                            worker.UniqueKey) &&
                                        (
                                            Same(
                                                transaction.Action,
                                                "Returned") ||
                                            Same(
                                                transaction.Action,
                                                "Returned Damaged")
                                        )),


                            Damages =
                                projectDamageReports.Count(
                                    report =>
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
                WorkerStats.Add(
                    worker);
            }


            MostActiveWorker =
                workerStats
                    .Where(worker =>
                        worker.TotalActivity > 0)
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
                        projectDamageReports
                            .Where(report =>
                                !string.IsNullOrWhiteSpace(
                                    report.ToolId))
                            .Select(report =>
                                report.ToolId))

                    .Concat(
                        projectLostReports
                            .Where(report =>
                                !string.IsNullOrWhiteSpace(
                                    report.ToolId))
                            .Select(report =>
                                report.ToolId))

                    .Concat(
                        projectTools
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


                            // Usage means equipment was
                            // received by a Worker.
                            Usage =
                                projectTransactions.Count(
                                    transaction =>
                                        Same(
                                            transaction.ToolId,
                                            tool.ToolId) &&
                                        (
                                            Same(
                                                transaction.Action,
                                                "Assignment Accepted") ||
                                            (
                                                Same(
                                                    transaction.Action,
                                                    "Borrowed") &&
                                                !string.IsNullOrWhiteSpace(
                                                    transaction.WorkerId)
                                            )
                                        )),


                            Damages =
                                projectDamageReports.Count(
                                    report =>
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
                ToolStats.Add(
                    tool);
            }


            MostUsedTool =
                toolStats
                    .Where(tool =>
                        tool.Usage > 0)
                    .OrderByDescending(tool =>
                        tool.Usage)
                    .ThenBy(tool =>
                        tool.ToolName)
                    .FirstOrDefault();


            // =====================================================
            // TOOLS INVOLVED IN MULTIPLE DAMAGE REPORTS
            // =====================================================

            HighRiskTools =
                projectDamageReports
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
                            _allTools
                                .FirstOrDefault(tool =>
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
            // WORKERS INVOLVED IN MULTIPLE DAMAGE REPORTS
            // =====================================================

            FrequentlyInvolvedWorkers =
                projectDamageReports
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
                            _allUsers
                                .FirstOrDefault(user =>
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
            RequiredTools = 0;
            BorrowedTools = 0;
            RemainingNeededTools = 0;
            DamagedTools = 0;
            LostTools = 0;

            TotalActivities = 0;
            ProjectBorrows = 0;
            WorkerReturns = 0;
            TotalCheckIns = 0;

            TotalReports = 0;
            PendingReports = 0;
            UnderRepairReports = 0;
            ResolvedReports = 0;

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