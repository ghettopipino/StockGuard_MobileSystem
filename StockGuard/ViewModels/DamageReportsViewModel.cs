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
            _theme.IsDark ? "🌙" : "☀️";

        // ─────────────────────────────────────────────────────────
        // STATS
        // ─────────────────────────────────────────────────────────

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

        // ─────────────────────────────────────────────────────────
        // REPORTS
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<DamageReportResult> Reports { get; }
            = new();

        // ─────────────────────────────────────────────────────────
        // EMPTY STATE
        // ─────────────────────────────────────────────────────────

        private bool _hasReports;

        public bool HasReports
        {
            get => _hasReports;
            private set
            {
                SetProperty(ref _hasReports, value);
                OnPropertyChanged(nameof(NoReports));
            }
        }

        public bool NoReports => !HasReports;

        // ─────────────────────────────────────────────────────────
        // FILTER
        // ─────────────────────────────────────────────────────────

        private string _selectedFilter = "All";

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    MainThread.BeginInvokeOnMainThread(
                        async () => await LoadReportsAsync());
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────────

        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // ─────────────────────────────────────────────────────────
        // COMMANDS
        // ─────────────────────────────────────────────────────────

        public ICommand OpenFlyoutCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand SetFilterCommand { get; }
        public ICommand HandleReportCommand { get; }

        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public DamageReportsViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;

            Title = "Damage Reports";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(
                    () => OnPropertyChanged(nameof(ThemeIcon)));

            OpenFlyoutCommand =
                new Command(() =>
                {
                    if (Shell.Current != null)
                    {
                        Shell.Current.FlyoutIsPresented = true;
                    }
                });

            RefreshCommand =
                new Command(
                    async () => await RefreshAsync());

            ToggleThemeCommand =
                new Command(
                    () => _theme.Toggle());

            SetFilterCommand =
                new Command<string>(
                    filter =>
                        SelectedFilter =
                            string.IsNullOrWhiteSpace(filter)
                                ? "All"
                                : filter);

            HandleReportCommand =
                new Command<DamageReportResult>(
                    async item =>
                        await HandleReportAsync(item));

            MainThread.BeginInvokeOnMainThread(
                async () => await LoadReportsAsync());
        }

        // ─────────────────────────────────────────────────────────
        // LOAD REPORTS
        // ─────────────────────────────────────────────────────────

        public async Task LoadReportsAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var user = _auth.CurrentUser;

                if (user == null)
                {
                    Reports.Clear();

                    TotalReports = 0;
                    PendingReports = 0;
                    ResolvedReports = 0;
                    HasReports = false;

                    return;
                }

                var reportsTask =
                    _firebase.GetAllDamageReportsRawAsync();

                var projectsTask =
                    _firebase.GetAllProjectsAsync();

                await Task.WhenAll(
                    reportsTask,
                    projectsTask);

                var rawReports =
                    reportsTask.Result ??
                    new List<DamageReportResult>();

                var projects =
                    projectsTask.Result ??
                    new List<Project>();

                // ── THIS PE'S PROJECTS ─────────────────────────────

                var myProjectIds =
                    projects
                        .Where(p =>
                            !p.IsDeleted &&
                            p.CreatedBy == user.UniqueKey)
                        .Select(p => p.ProjectId)
                        .ToHashSet();

                // ── REPORTS BELONGING TO THIS PE'S PROJECTS ───────

                var ownedReports =
                    rawReports
                        .Where(r =>
                            myProjectIds.Contains(
                                r.Report.ProjectId))
                        .ToList();

                // ── STATS ─────────────────────────────────────────

                TotalReports =
                    ownedReports.Count;

                PendingReports =
                    ownedReports.Count(r =>
                        r.Report.Status == "Pending");

                ResolvedReports =
                    ownedReports.Count(r =>
                        r.Report.Status == "Resolved");

                // ── FILTER ────────────────────────────────────────

                IEnumerable<DamageReportResult> filtered =
                    ownedReports;

                if (SelectedFilter != "All")
                {
                    filtered =
                        filtered.Where(r =>
                            r.Report.Status ==
                            SelectedFilter);
                }

                // ── DISPLAY ───────────────────────────────────────

                Reports.Clear();

                foreach (var item in filtered
                    .OrderByDescending(r =>
                        r.Report.ReportDate))
                {
                    Reports.Add(item);
                }

                HasReports =
                    Reports.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadReports error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────────

        private async Task RefreshAsync()
        {
            IsRefreshing = true;

            try
            {
                await LoadReportsAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // HANDLE REPORT
        // ─────────────────────────────────────────────────────────

        private async Task HandleReportAsync(
            DamageReportResult item)
        {
            if (item == null || IsBusy)
                return;

            var report = item.Report;

            // Resolved is the only completely finished state.
            if (report.Status == "Resolved")
            {
                await Shell.Current.DisplayAlert(
                    "Already Resolved",
                    "This damage report has already been resolved.",
                    "OK");

                return;
            }

            // ─────────────────────────────────────────────────────
            // LOST EQUIPMENT
            // ─────────────────────────────────────────────────────

            if (report.Status == "Lost")
            {
                await HandleFoundEquipmentAsync(item);
                return;
            }

            // ─────────────────────────────────────────────────────
            // NORMAL DAMAGE ACTIONS
            // ─────────────────────────────────────────────────────

            var action =
                await Shell.Current.DisplayActionSheet(
                    $"Handle {report.ToolName}",
                    "Cancel",
                    null,
                    "Send to Repair",
                    "Mark Ready for Use",
                    "Mark as Lost");

            if (string.IsNullOrWhiteSpace(action) ||
                action == "Cancel")
            {
                return;
            }

            string? notes =
                await Shell.Current.DisplayPromptAsync(
                    "Resolution Notes",
                    "Add a short note about the action taken:",
                    "Save",
                    "Skip",
                    placeholder:
                    "e.g. Sent to maintenance for cable replacement");

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Confirm Action",
                    $"{report.ToolName} ({report.ToolId})\n\n" +
                    $"Action: {action}",
                    "Confirm",
                    "Cancel");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                var user = _auth.CurrentUser;

                if (user == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Current Project Engineer could not be identified.",
                        "OK");

                    return;
                }

                var tool =
                    await _firebase.GetToolByIdAsync(
                        report.ToolId);

                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The equipment could not be found.",
                        "OK");

                    return;
                }

                // ── DETERMINE NEW STATUS ──────────────────────────

                switch (action)
                {
                    case "Send to Repair":

                        report.Status = "UnderRepair";
                        tool.Status = "UnderRepair";

                        break;

                    case "Mark Ready for Use":

                        report.Status = "Resolved";

                        tool.Status = "Available";
                        tool.Condition = "Good";

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

                        break;

                    case "Mark as Lost":

                        report.Status = "Lost";

                        tool.Status = "Lost";

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

                        break;

                    default:
                        return;
                }

                // ── REVIEW DETAILS ─────────────────────────────────

                report.ReviewedDate =
                    DateTime.Now;

                report.ReviewedById =
                    user.UniqueKey;

                report.ReviewedByName =
                    user.FullName;

                report.ResolutionNotes =
                    notes?.Trim() ??
                    string.Empty;

                // ── UPDATE REPORT ──────────────────────────────────

                var reportUpdated =
                    await _firebase.UpdateDamageReportAsync(
                        item.Key,
                        report);

                if (!reportUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not update the damage report.",
                        "OK");

                    return;
                }

                // ── UPDATE TOOL ────────────────────────────────────

                var toolUpdated =
                    await _firebase.UpdateToolAsync(tool);

                if (!toolUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Damage report was updated, but the equipment status could not be updated.",
                        "OK");

                    return;
                }

                // ── TRANSACTION ────────────────────────────────────

                await _firebase.LogTransactionAsync(
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

                await Shell.Current.DisplayAlert(
                    "Report Updated",
                    $"{report.ToolName} ({report.ToolId})\n\n" +
                    $"Status: {report.Status}",
                    "OK");

                await LoadReportsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not handle damage report.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // FOUND EQUIPMENT
        // ─────────────────────────────────────────────────────────

        private async Task HandleFoundEquipmentAsync(
            DamageReportResult item)
        {
            var report = item.Report;

            var action =
                await Shell.Current.DisplayActionSheet(
                    $"Lost Equipment - {report.ToolName}",
                    "Cancel",
                    null,
                    "Mark as Found");

            if (string.IsNullOrWhiteSpace(action) ||
                action == "Cancel")
            {
                return;
            }

            var condition =
                await Shell.Current.DisplayActionSheet(
                    "Equipment Found",
                    "Cancel",
                    null,
                    "Good",
                    "Damaged");

            if (string.IsNullOrWhiteSpace(condition) ||
                condition == "Cancel")
            {
                return;
            }

            string? notes =
                await Shell.Current.DisplayPromptAsync(
                    "Found Equipment",
                    "Where was the equipment found or what happened?",
                    "Save",
                    "Skip",
                    placeholder:
                    "e.g. Found inside the project storage area");

            string resultText =
                condition == "Good"
                    ? "Available"
                    : "Under Repair";

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Confirm Found Equipment",
                    $"{report.ToolName} ({report.ToolId})\n\n" +
                    $"Condition: {condition}\n" +
                    $"Result: {resultText}",
                    "Confirm",
                    "Cancel");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                var user = _auth.CurrentUser;

                if (user == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Current Project Engineer could not be identified.",
                        "OK");

                    return;
                }

                var tool =
                    await _firebase.GetToolByIdAsync(
                        report.ToolId);

                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The equipment could not be found.",
                        "OK");

                    return;
                }

                // ── FOUND AND GOOD ─────────────────────────────────

                if (condition == "Good")
                {
                    report.Status =
                        "Resolved";

                    tool.Status =
                        "Available";

                    tool.Condition =
                        "Good";
                }

                // ── FOUND BUT STILL DAMAGED ───────────────────────

                else
                {
                    report.Status =
                        "UnderRepair";

                    tool.Status =
                        "UnderRepair";
                }

                // A lost tool already had its current assignment
                // cleared when it was marked lost.
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

                // ── REVIEW DETAILS ─────────────────────────────────

                report.ReviewedDate =
                    DateTime.Now;

                report.ReviewedById =
                    user.UniqueKey;

                report.ReviewedByName =
                    user.FullName;

                report.ResolutionNotes =
                    notes?.Trim() ??
                    string.Empty;

                // ── UPDATE REPORT ──────────────────────────────────

                var reportUpdated =
                    await _firebase.UpdateDamageReportAsync(
                        item.Key,
                        report);

                if (!reportUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not update the damage report.",
                        "OK");

                    return;
                }

                // ── UPDATE TOOL ────────────────────────────────────

                var toolUpdated =
                    await _firebase.UpdateToolAsync(tool);

                if (!toolUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The report was updated, but the equipment status could not be updated.",
                        "OK");

                    return;
                }

                // ── TRANSACTION ────────────────────────────────────

                await _firebase.LogTransactionAsync(
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
                            "Equipment Found",

                        Description =
                            $"{report.ToolName} was found. " +
                            $"Condition: {condition}. " +
                            $"{report.ResolutionNotes}",

                        Condition =
                            tool.Condition,

                        Date =
                            DateTime.Now
                    });

                await Shell.Current.DisplayAlert(
                    "Equipment Found",
                    condition == "Good"
                        ? $"{report.ToolName} ({report.ToolId}) is now available for use."
                        : $"{report.ToolName} ({report.ToolId}) was found but still requires repair.",
                    "OK");

                await LoadReportsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not update the found equipment.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}