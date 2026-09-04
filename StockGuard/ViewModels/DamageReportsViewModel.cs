using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    public class DamageReportsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        // ─────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────

        public string ThemeIcon =>
            _theme.IsDark
                ? "🌙"
                : "☀️";

        // ─────────────────────────────────────────────────────────
        // STATS
        // ─────────────────────────────────────────────────────────

        private int _totalReports;

        public int TotalReports
        {
            get =>
                _totalReports;

            private set =>
                SetProperty(
                    ref _totalReports,
                    value);
        }

        private int _pendingReports;

        public int PendingReports
        {
            get =>
                _pendingReports;

            private set =>
                SetProperty(
                    ref _pendingReports,
                    value);
        }

        private int _resolvedReports;

        public int ResolvedReports
        {
            get =>
                _resolvedReports;

            private set =>
                SetProperty(
                    ref _resolvedReports,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // COLLECTIONS
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<
            DamageReportResult>
            DamageReports
        { get; }
            = new();

        public ObservableCollection<
            LostReportResult>
            LostReports
        { get; }
            = new();

        // ─────────────────────────────────────────────────────────
        // FILTER
        // ─────────────────────────────────────────────────────────

        private string _selectedFilter =
            "All";

        public string SelectedFilter
        {
            get =>
                _selectedFilter;

            set
            {
                if (SetProperty(
                        ref _selectedFilter,
                        value))
                {
                    OnPropertyChanged(
                        nameof(ShowDamageSection));

                    OnPropertyChanged(
                        nameof(ShowLostSection));

                    MainThread.BeginInvokeOnMainThread(
                        async () =>
                            await LoadReportsAsync());
                }
            }
        }

        public bool ShowDamageSection =>
            SelectedFilter ==
                "All" ||
            SelectedFilter ==
                "Damage";

        public bool ShowLostSection =>
            SelectedFilter ==
                "All" ||
            SelectedFilter ==
                "Lost";

        // ─────────────────────────────────────────────────────────
        // VISIBILITY
        // ─────────────────────────────────────────────────────────

        private bool _hasDamageReports;

        public bool HasDamageReports
        {
            get =>
                _hasDamageReports;

            private set
            {
                SetProperty(
                    ref _hasDamageReports,
                    value);

                OnPropertyChanged(
                    nameof(NoReports));
            }
        }

        private bool _hasLostReports;

        public bool HasLostReports
        {
            get =>
                _hasLostReports;

            private set
            {
                SetProperty(
                    ref _hasLostReports,
                    value);

                OnPropertyChanged(
                    nameof(NoReports));
            }
        }

        public bool NoReports =>
            !HasDamageReports &&
            !HasLostReports;

        // ─────────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────────

        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get =>
                _isRefreshing;

            set =>
                SetProperty(
                    ref _isRefreshing,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // COMMANDS
        // ─────────────────────────────────────────────────────────

        public ICommand OpenFlyoutCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand SetFilterCommand { get; }

        public ICommand HandleDamageReportCommand { get; }
        public ICommand HandleLostReportCommand { get; }

        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public DamageReportsViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase =
                firebase;

            _auth =
                auth;

            _theme =
                theme;

            Title =
                "Damage & Lost Reports";

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
                            Shell.Current
                                .FlyoutIsPresented =
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

            SetFilterCommand =
                new Command<string>(
                    filter =>
                        SelectedFilter =
                            string.IsNullOrWhiteSpace(
                                filter)
                                ? "All"
                                : filter);

            HandleDamageReportCommand =
                new Command<DamageReportResult>(
                    async item =>
                        await HandleDamageReportAsync(
                            item));

            HandleLostReportCommand =
                new Command<LostReportResult>(
                    async item =>
                        await HandleLostReportAsync(
                            item));

            MainThread.BeginInvokeOnMainThread(
                async () =>
                    await LoadReportsAsync());
        }

        // ─────────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────────

        public async Task LoadReportsAsync()
        {
            if (IsBusy)
                return;

            IsBusy =
                true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                {
                    DamageReports.Clear();
                    LostReports.Clear();

                    TotalReports = 0;
                    PendingReports = 0;
                    ResolvedReports = 0;

                    HasDamageReports =
                        false;

                    HasLostReports =
                        false;

                    return;
                }

                var damageTask =
                    _firebase
                        .GetAllDamageReportsRawAsync();

                var lostTask =
                    _firebase
                        .GetAllLostReportsRawAsync();

                var projectsTask =
                    _firebase
                        .GetAllProjectsAsync();

                await Task.WhenAll(
                    damageTask,
                    lostTask,
                    projectsTask);

                var damageReports =
                    damageTask.Result ??
                    new List<
                        DamageReportResult>();

                var lostReports =
                    lostTask.Result ??
                    new List<
                        LostReportResult>();

                var projects =
                    projectsTask.Result ??
                    new List<Project>();

                var myProjectIds =
                    projects
                        .Where(p =>
                            !p.IsDeleted &&
                            p.CreatedBy ==
                                user.UniqueKey)
                        .Select(p =>
                            p.ProjectId)
                        .ToHashSet();

                var ownedDamage =
                    damageReports
                        .Where(r =>
                            myProjectIds.Contains(
                                r.Report.ProjectId))
                        .ToList();

                var ownedLost =
                    lostReports
                        .Where(r =>
                            myProjectIds.Contains(
                                r.Report.ProjectId))
                        .ToList();

                TotalReports =
                    ownedDamage.Count +
                    ownedLost.Count;

                PendingReports =
                    ownedDamage.Count(r =>
                        r.Report.Status ==
                            "Pending") +

                    ownedLost.Count(r =>
                        r.Report.Status ==
                            "Pending");

                ResolvedReports =
                    ownedDamage.Count(r =>
                        r.Report.Status ==
                            "Resolved") +

                    ownedLost.Count(r =>
                        r.Report.Status ==
                            "Resolved");

                DamageReports.Clear();
                LostReports.Clear();

                if (ShowDamageSection)
                {
                    foreach (var item in
                        ownedDamage
                            .OrderByDescending(r =>
                                r.Report.ReportDate))
                    {
                        DamageReports.Add(
                            item);
                    }
                }

                if (ShowLostSection)
                {
                    foreach (var item in
                        ownedLost
                            .OrderByDescending(r =>
                                r.Report.ReportDate))
                    {
                        LostReports.Add(
                            item);
                    }
                }

                HasDamageReports =
                    DamageReports.Count >
                    0;

                HasLostReports =
                    LostReports.Count >
                    0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug
                    .WriteLine(
                        $"LoadReports error: {ex.Message}");
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing =
                true;

            try
            {
                await LoadReportsAsync();
            }
            finally
            {
                IsRefreshing =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // DAMAGE REPORT
        // ─────────────────────────────────────────────────────────

        private async Task HandleDamageReportAsync(
            DamageReportResult item)
        {
            if (item == null ||
                IsBusy)
            {
                return;
            }

            var report =
                item.Report;

            if (report.Status ==
                "Resolved")
            {
                await Shell.Current
                    .DisplayAlert(
                        "Already Resolved",
                        "This damage report has already been resolved.",
                        "OK");

                return;
            }

            // IMPORTANT:
            // NO LOST OPTION HERE.
            var action =
                await Shell.Current
                    .DisplayActionSheet(
                        $"Handle {report.ToolName}",
                        "Cancel",
                        null,
                        "Send to Repair",
                        "Mark Ready for Use");

            if (string.IsNullOrWhiteSpace(
                    action) ||
                action ==
                    "Cancel")
            {
                return;
            }

            string? notes =
                await Shell.Current
                    .DisplayPromptAsync(
                        "Resolution Notes",
                        "Add a short note about the action taken:",
                        "Save",
                        "Skip");

            bool confirm =
                await Shell.Current
                    .DisplayAlert(
                        "Confirm Action",
                        $"{report.ToolName} ({report.ToolId})\n\n" +
                        $"Action: {action}",
                        "Confirm",
                        "Cancel");

            if (!confirm)
                return;

            IsBusy =
                true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                    return;

                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            report.ToolId);

                if (tool == null)
                    return;

                switch (action)
                {
                    case "Send to Repair":

                        report.Status =
                            "UnderRepair";

                        tool.Status =
                            "UnderRepair";

                        break;

                    case "Mark Ready for Use":

                        report.Status =
                            "Resolved";

                        tool.Status =
                            "Available";

                        tool.Condition =
                            "Good";

                        ClearCurrentAssignment(
                            tool);

                        break;

                    default:
                        return;
                }

                report.ReviewedDate =
                    DateTime.Now;

                report.ReviewedById =
                    user.UniqueKey;

                report.ReviewedByName =
                    user.FullName;

                report.ResolutionNotes =
                    notes?.Trim() ??
                    string.Empty;

                var reportUpdated =
                    await _firebase
                        .UpdateDamageReportAsync(
                            item.Key,
                            report);

                if (!reportUpdated)
                    return;

                var toolUpdated =
                    await _firebase
                        .UpdateToolAsync(
                            tool);

                if (!toolUpdated)
                    return;

                await _firebase
                    .LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId =
                                tool.ToolId,

                            ToolName =
                                tool.ToolName,

                            WorkerId =
                                report.WorkerId,

                            WorkerName =
                                report.WorkerName,

                            ProjectId =
                                report.ProjectId,

                            ProjectName =
                                report.ProjectName,

                            PerformedById =
                                user.UniqueKey,

                            PerformedByName =
                                user.FullName,

                            Action =
                                report.Status,

                            Description =
                                $"Damage report handled by " +
                                $"{user.FullName}. " +
                                $"{action}. " +
                                $"{report.ResolutionNotes}",

                            Condition =
                                tool.Condition,

                            Date =
                                DateTime.Now
                        });

                await Shell.Current
                    .DisplayAlert(
                        "Report Updated",
                        $"{report.ToolName}\n\n" +
                        $"Status: {report.Status}",
                        "OK");

                await LoadReportsAsync();
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // LOST REPORT
        // ─────────────────────────────────────────────────────────

        private async Task HandleLostReportAsync(
            LostReportResult item)
        {
            if (item == null ||
                IsBusy)
            {
                return;
            }

            var report =
                item.Report;

            if (report.Status ==
                "Resolved")
            {
                await Shell.Current
                    .DisplayAlert(
                        "Already Resolved",
                        "This missing/lost report has already been resolved.",
                        "OK");

                return;
            }

            // Worker reported missing,
            // PE has not declared Lost yet.
            if (report.Status ==
                "Pending")
            {
                var action =
                    await Shell.Current
                        .DisplayActionSheet(
                            $"Missing Equipment - {report.ToolName}",
                            "Cancel",
                            null,
                            "Found",
                            "Declare Lost");

                if (action ==
                    "Found")
                {
                    await HandleFoundAsync(
                        item,
                        false);

                    return;
                }

                if (action ==
                    "Declare Lost")
                {
                    await DeclareLostAsync(
                        item);

                    return;
                }

                return;
            }

            // Already officially Lost.
            if (report.Status ==
                "Lost")
            {
                var action =
                    await Shell.Current
                        .DisplayActionSheet(
                            $"Lost Equipment - {report.ToolName}",
                            "Cancel",
                            null,
                            "Mark as Found");

                if (action !=
                    "Mark as Found")
                {
                    return;
                }

                await HandleFoundAsync(
                    item,
                    true);
            }
        }

        // ─────────────────────────────────────────────────────────
        // DECLARE LOST
        // ─────────────────────────────────────────────────────────

        private async Task DeclareLostAsync(
            LostReportResult item)
        {
            var report =
                item.Report;

            string? notes =
                await Shell.Current
                    .DisplayPromptAsync(
                        "Declare Lost",
                        "Enter verification notes or the reason for declaring this equipment lost:",
                        "Continue",
                        "Cancel");

            if (string.IsNullOrWhiteSpace(
                    notes))
            {
                return;
            }

            bool confirm =
                await Shell.Current
                    .DisplayAlert(
                        "Confirm Lost Declaration",
                        $"{report.ToolName} ({report.ToolId})\n\n" +
                        "This will officially mark the equipment as Lost.",
                        "Declare Lost",
                        "Cancel");

            if (!confirm)
                return;

            IsBusy =
                true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                    return;

                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            report.ToolId);

                if (tool == null)
                    return;

                report.Status =
                    "Lost";

                report.VerifiedDate =
                    DateTime.Now;

                report.VerifiedById =
                    user.UniqueKey;

                report.VerifiedByName =
                    user.FullName;

                report.LostDate =
                    DateTime.Now;

                report.ResolutionNotes =
                    notes.Trim();

                tool.Status =
                    "Lost";

                // Active assignment is now cleared.
                // Historical accountability remains
                // inside LostReport.
                ClearCurrentAssignment(
                    tool);

                var reportUpdated =
                    await _firebase
                        .UpdateLostReportAsync(
                            item.Key,
                            report);

                if (!reportUpdated)
                    return;

                var toolUpdated =
                    await _firebase
                        .UpdateToolAsync(
                            tool);

                if (!toolUpdated)
                    return;

                await _firebase
                    .LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId =
                                report.ToolId,

                            ToolName =
                                report.ToolName,

                            WorkerId =
                                report.WorkerId,

                            WorkerName =
                                report.WorkerName,

                            ProjectId =
                                report.ProjectId,

                            ProjectName =
                                report.ProjectName,

                            PerformedById =
                                user.UniqueKey,

                            PerformedByName =
                                user.FullName,

                            Action =
                                "Lost Declared",

                            Description =
                                $"Project Engineer declared the equipment lost. " +
                                $"{report.ResolutionNotes}",

                            Condition =
                                tool.Condition,

                            Date =
                                DateTime.Now
                        });

                await Shell.Current
                    .DisplayAlert(
                        "Equipment Declared Lost",
                        $"{report.ToolName} ({report.ToolId}) " +
                        "has been officially marked as Lost.",
                        "OK");

                await LoadReportsAsync();
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // FOUND EQUIPMENT
        // ─────────────────────────────────────────────────────────

        private async Task HandleFoundAsync(
            LostReportResult item,
            bool wasDeclaredLost)
        {
            var report =
                item.Report;

            var condition =
                await Shell.Current
                    .DisplayActionSheet(
                        "Equipment Found",
                        "Cancel",
                        null,
                        "Good",
                        "Damaged");

            if (string.IsNullOrWhiteSpace(
                    condition) ||
                condition ==
                    "Cancel")
            {
                return;
            }

            string severity =
                string.Empty;

            string damageDescription =
                string.Empty;

            // PE evaluates damage.
            if (condition ==
                "Damaged")
            {
                severity =
                    await Shell.Current
                        .DisplayActionSheet(
                            "Damage Severity",
                            "Cancel",
                            null,
                            "Minor",
                            "Major");

                if (string.IsNullOrWhiteSpace(
                        severity) ||
                    severity ==
                        "Cancel")
                {
                    return;
                }

                damageDescription =
                    await Shell.Current
                        .DisplayPromptAsync(
                            "Damage Description",
                            "Describe the damage found during inspection:",
                            "Continue",
                            "Cancel")
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(
                        damageDescription))
                {
                    return;
                }

                damageDescription =
                    damageDescription.Trim();
            }

            string? notes =
                await Shell.Current
                    .DisplayPromptAsync(
                        "Found Equipment",
                        "Where was the equipment found or what happened?",
                        "Save",
                        "Skip");

            bool confirm =
                await Shell.Current
                    .DisplayAlert(
                        "Confirm Found Equipment",
                        $"{report.ToolName} ({report.ToolId})\n\n" +
                        $"Condition: {condition}",
                        "Confirm",
                        "Cancel");

            if (!confirm)
                return;

            IsBusy =
                true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                    return;

                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            report.ToolId);

                if (tool == null)
                    return;

                report.Status =
                    "Resolved";

                report.VerifiedDate ??=
                    DateTime.Now;

                report.VerifiedById =
                    user.UniqueKey;

                report.VerifiedByName =
                    user.FullName;

                report.FoundDate =
                    DateTime.Now;

                report.FoundCondition =
                    condition;

                report.ResolutionNotes =
                    notes?.Trim() ??
                    string.Empty;

                // ─────────────────────────────────────
                // FOUND + GOOD
                // ─────────────────────────────────────

                if (condition ==
                    "Good")
                {
                    tool.Condition =
                        "Good";

                    // It was officially Lost before recovery.
                    if (wasDeclaredLost)
                    {
                        tool.Status =
                            "Available";

                        ClearCurrentAssignment(
                            tool);
                    }

                    // Worker only reported Missing.
                    // PE found it before declaring Lost.
                    else
                    {
                        tool.Status =
                            "Borrowed";

                        // Keep worker/project.
                    }
                }

                // ─────────────────────────────────────
                // FOUND + DAMAGED
                // ─────────────────────────────────────

                else
                {
                    tool.Status =
                        "Damaged";

                    tool.Condition =
                        severity;

                    // If previously declared Lost,
                    // its active assignment was already cleared.
                    if (wasDeclaredLost)
                    {
                        ClearCurrentAssignment(
                            tool);
                    }

                    // If only Pending Missing,
                    // worker/project remain attached.
                }

                var lostUpdated =
                    await _firebase
                        .UpdateLostReportAsync(
                            item.Key,
                            report);

                if (!lostUpdated)
                    return;

                var toolUpdated =
                    await _firebase
                        .UpdateToolAsync(
                            tool);

                if (!toolUpdated)
                    return;

                // IMPORTANT:
                // LostReport is resolved.
                // A NEW DamageReport is created.
                if (condition ==
                    "Damaged")
                {
                    var damageReport =
                        new DamageReport
                        {
                            ToolId =
                                report.ToolId,

                            ToolName =
                                report.ToolName,

                            WorkerId =
                                report.WorkerId,

                            WorkerName =
                                report.WorkerName,

                            ProjectId =
                                report.ProjectId,

                            ProjectName =
                                report.ProjectName,

                            ProjectEngineerId =
                                user.UniqueKey,

                            ProjectEngineerName =
                                user.FullName,

                            Description =
                                damageDescription,

                            Severity =
                                severity,

                            Status =
                                "Pending",

                            ReportDate =
                                DateTime.Now
                        };

                    await _firebase
                        .SubmitDamageReportAsync(
                            damageReport);
                }

                await _firebase
                    .LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId =
                                report.ToolId,

                            ToolName =
                                report.ToolName,

                            WorkerId =
                                report.WorkerId,

                            WorkerName =
                                report.WorkerName,

                            ProjectId =
                                report.ProjectId,

                            ProjectName =
                                report.ProjectName,

                            PerformedById =
                                user.UniqueKey,

                            PerformedByName =
                                user.FullName,

                            Action =
                                "Equipment Found",

                            Description =
                                condition ==
                                    "Good"

                                    ? $"Equipment found and inspected as Good. " +
                                      $"{report.ResolutionNotes}"

                                    : $"Equipment found and inspected as Damaged ({severity}). " +
                                      $"A separate Damage Report was created. " +
                                      $"{report.ResolutionNotes}",

                            Condition =
                                tool.Condition,

                            Date =
                                DateTime.Now
                        });

                await Shell.Current
                    .DisplayAlert(
                        "Equipment Found",

                        condition ==
                            "Good"

                            ? wasDeclaredLost
                                ? $"{report.ToolName} is now Available."
                                : $"{report.ToolName} was found and remains assigned to {report.WorkerName}."

                            : $"{report.ToolName} was found damaged. A separate Damage Report was created.",

                        "OK");

                await LoadReportsAsync();
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // CLEAR ACTIVE ASSIGNMENT
        // ─────────────────────────────────────────────────────────

        private static void ClearCurrentAssignment(
            Tool tool)
        {
            tool.AssignedWorkerId =
                string.Empty;

            tool.AssignedWorkerName =
                string.Empty;

            tool.BorrowedProjectId =
                string.Empty;

            tool.BorrowedProjectName =
                string.Empty;

            tool.BorrowDate =
                null;
        }
    }
}