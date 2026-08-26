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

        public ObservableCollection<DamageReportResult>
            Reports
        { get; } = new();

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

        public bool NoReports =>
            !HasReports;

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
                    () =>
                        OnPropertyChanged(
                            nameof(ThemeIcon)));

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
                    async () =>
                        await RefreshAsync());

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
                async () =>
                    await LoadReportsAsync());
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

                // Load damage reports and projects
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

                IEnumerable<DamageReportResult>
                    filtered = ownedReports;

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

            var report =
                item.Report;

            if (report.Status == "Resolved" ||
                report.Status == "Lost")
            {
                await Shell.Current.DisplayAlert(
                    "Already Handled",
                    "This damage report has already been handled.",
                    "OK");

                return;
            }

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
                var user =
                    _auth.CurrentUser;

                if (user == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Current Project Engineer could not be identified.",
                        "OK");

                    return;
                }

                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            report.ToolId);

                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The equipment could not be found.",
                        "OK");

                    return;
                }

                // ── DETERMINE NEW STATUS ───────────────────

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

                        // Equipment is ready for company use again.
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

                        report.Status =
                            "Lost";

                        tool.Status =
                            "Lost";

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

                // ── REVIEW DETAILS ─────────────────────────

                report.ReviewedDate =
                    DateTime.Now;

                report.ReviewedById =
                    user.UniqueKey;

                report.ReviewedByName =
                    user.FullName;

                report.ResolutionNotes =
                    notes?.Trim() ??
                    string.Empty;

                // ── UPDATE REPORT ──────────────────────────

                var reportUpdated =
                    await _firebase
                        .UpdateDamageReportAsync(
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

                // ── UPDATE TOOL ────────────────────────────

                var toolUpdated =
                    await _firebase
                        .UpdateToolAsync(tool);

                if (!toolUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Damage report was updated, but the equipment status could not be updated.",
                        "OK");

                    return;
                }


                // ── TRANSACTION ────────────────────────────

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

                            // THIS IS THE IMPORTANT FIX
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


    }
}