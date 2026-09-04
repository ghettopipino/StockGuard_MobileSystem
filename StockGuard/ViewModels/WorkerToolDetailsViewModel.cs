using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    [QueryProperty(nameof(ToolId), "toolId")]
    public class WorkerToolDetailsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        // ─────────────────────────────────────────────────────────
        // QUERY PROPERTY
        // ─────────────────────────────────────────────────────────

        private string _toolId = string.Empty;

        public string ToolId
        {
            get => _toolId;

            set
            {
                SetProperty(
                    ref _toolId,
                    value);

                if (!string.IsNullOrWhiteSpace(
                        value))
                {
                    MainThread.BeginInvokeOnMainThread(
                        async () =>
                            await LoadToolAsync());
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // TOOL
        // ─────────────────────────────────────────────────────────

        private Tool? _tool;

        public Tool? Tool
        {
            get => _tool;

            set
            {
                SetProperty(
                    ref _tool,
                    value);

                RefreshToolProperties();
            }
        }

        // ─────────────────────────────────────────────────────────
        // ACTIVE MISSING REPORT
        // ─────────────────────────────────────────────────────────

        private bool _hasActiveMissingReport;

        public bool HasActiveMissingReport
        {
            get =>
                _hasActiveMissingReport;

            private set
            {
                if (SetProperty(
                        ref _hasActiveMissingReport,
                        value))
                {
                    RefreshToolProperties();
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // REFRESH PROPERTIES
        // ─────────────────────────────────────────────────────────

        private void RefreshToolProperties()
        {
            OnPropertyChanged(
                nameof(ToolName));

            OnPropertyChanged(
                nameof(ToolIdDisplay));

            OnPropertyChanged(
                nameof(ToolIcon));

            OnPropertyChanged(
                nameof(StatusText));

            OnPropertyChanged(
                nameof(StatusColor));

            OnPropertyChanged(
                nameof(StatusIcon));

            OnPropertyChanged(
                nameof(AssignedWorkerName));

            OnPropertyChanged(
                nameof(ProjectName));

            OnPropertyChanged(
                nameof(AssignedByName));

            OnPropertyChanged(
                nameof(BorrowDateDisplay));

            OnPropertyChanged(
                nameof(ConditionText));

            OnPropertyChanged(
                nameof(IsAssignedToMe));

            OnPropertyChanged(
                nameof(ShowBorrow));

            OnPropertyChanged(
                nameof(ShowReturn));

            OnPropertyChanged(
                nameof(ShowPendingReturn));

            OnPropertyChanged(
                nameof(ShowTransfer));

            OnPropertyChanged(
                nameof(ShowRequestBorrow));

            OnPropertyChanged(
                nameof(ShowEndDayCheckIn));

            OnPropertyChanged(
                nameof(ShowPendingCheckIn));

            OnPropertyChanged(
                nameof(ShowConfirmReceipt));

            OnPropertyChanged(
                nameof(ShowDeclineReceipt));

            OnPropertyChanged(
                nameof(ShowReportMissing));

            OnPropertyChanged(
                nameof(ShowMissingPending));

            OnPropertyChanged(
                nameof(CheckInLocation));

            OnPropertyChanged(
                nameof(CheckInDateDisplay));
        }

        // ─────────────────────────────────────────────────────────
        // DISPLAY PROPERTIES
        // ─────────────────────────────────────────────────────────

        public string ToolName =>
            Tool?.ToolName ??
            "Loading...";

        public string ToolIdDisplay =>
            Tool?.ToolId ??
            string.Empty;

        public string ToolIcon =>
            Tool?.ToolIcon ??
            "🔧";

        public string StatusText =>
            Tool?.Status ??
            string.Empty;

        public string StatusColor =>
            Tool?.StatusColor ??
            "#6b7280";

        public string StatusIcon =>
            Tool?.StatusIcon ??
            "❓";

        public string ThemeIcon =>
            _theme.IsDark
                ? "🌙"
                : "☀️";

        public string AssignedWorkerName =>
            string.IsNullOrWhiteSpace(
                Tool?.AssignedWorkerName)
                ? "— Not assigned —"
                : Tool.AssignedWorkerName;

        public string ProjectName =>
            string.IsNullOrWhiteSpace(
                Tool?.BorrowedProjectName)
                ? "—"
                : Tool.BorrowedProjectName;

        public string AssignedByName =>
            string.IsNullOrWhiteSpace(
                Tool?.AssignedByName)
                ? "—"
                : Tool.AssignedByName;

        public string BorrowDateDisplay =>
            Tool?.BorrowDate.HasValue ==
            true
                ? Tool.BorrowDate
                    .Value
                    .ToString(
                        "MMM d, yyyy h:mm tt")
                : "— Not borrowed —";

        public string ConditionText =>
            string.IsNullOrWhiteSpace(
                Tool?.Condition)
                ? "Good"
                : Tool.Condition;

        // ─────────────────────────────────────────────────────────
        // CURRENT USER
        // ─────────────────────────────────────────────────────────

        private string CurrentUserKey =>
            _auth.CurrentUser
                ?.UniqueKey ??
            string.Empty;

        public bool IsAssignedToMe =>
            Tool != null &&

            !string.IsNullOrWhiteSpace(
                Tool.AssignedWorkerId) &&

            string.Equals(
                Tool.AssignedWorkerId.Trim(),
                CurrentUserKey.Trim(),
                StringComparison.OrdinalIgnoreCase);

        // ─────────────────────────────────────────────────────────
        // ACTION VISIBILITY
        // ─────────────────────────────────────────────────────────

        public bool ShowBorrow =>
            Tool != null &&
            Tool.Status ==
                "Available";

        public bool ShowReturn =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status ==
                "Borrowed" &&
            !HasActiveMissingReport;

        public bool ShowPendingReturn =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status ==
                "PendingReturn";

        public bool ShowEndDayCheckIn =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status ==
                "Borrowed" &&
            !Tool.IsCheckInPending &&
            !HasActiveMissingReport;

        public bool ShowPendingCheckIn =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status ==
                "Borrowed" &&
            Tool.IsCheckInPending;

        public bool ShowTransfer =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status ==
                "Borrowed" &&
            !HasActiveMissingReport;

        public bool ShowRequestBorrow =>
            Tool != null &&
            Tool.Status ==
                "Borrowed" &&
            !IsAssignedToMe;

        public bool ShowConfirmReceipt =>
            Tool != null &&

            string.Equals(
                Tool.PreAssignedWorkerId
                    ?.Trim(),
                CurrentUserKey.Trim(),
                StringComparison.OrdinalIgnoreCase) &&

            Tool.Status ==
                "Available";

        public bool ShowDeclineReceipt =>
            ShowConfirmReceipt;

        // Worker can report missing only
        // when responsible for the borrowed tool.
        public bool ShowReportMissing =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status ==
                "Borrowed" &&
            !HasActiveMissingReport;

        public bool ShowMissingPending =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status ==
                "Borrowed" &&
            HasActiveMissingReport;

        // ─────────────────────────────────────────────────────────
        // CHECK-IN DISPLAY
        // ─────────────────────────────────────────────────────────

        public string CheckInLocation =>
            string.IsNullOrWhiteSpace(
                Tool?.LastCheckInLocation)
                ? "—"
                : Tool.LastCheckInLocation;

        public string CheckInDateDisplay =>
            Tool?.LastCheckInDate
                .HasValue ==
            true
                ? Tool.LastCheckInDate
                    .Value
                    .ToString(
                        "MMM d, yyyy h:mm tt")
                : "—";

        // ─────────────────────────────────────────────────────────
        // LOADING
        // ─────────────────────────────────────────────────────────

        private bool _isLoading;

        public bool IsLoading
        {
            get =>
                _isLoading;

            set
            {
                SetProperty(
                    ref _isLoading,
                    value);

                OnPropertyChanged(
                    nameof(IsNotLoading));
            }
        }

        public bool IsNotLoading =>
            !IsLoading;

        private bool _toolNotFound;

        public bool ToolNotFound
        {
            get =>
                _toolNotFound;

            set =>
                SetProperty(
                    ref _toolNotFound,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // COMMANDS
        // ─────────────────────────────────────────────────────────

        public ICommand BorrowCommand { get; }
        public ICommand ReturnCommand { get; }
        public ICommand TransferCommand { get; }

        public ICommand RequestBorrowCommand { get; }

        public ICommand ConfirmReceiptCommand { get; }
        public ICommand DeclineCommand { get; }

        public ICommand GoBackCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewHistoryCommand { get; }

        public ICommand EndDayCheckInCommand { get; }

        public ICommand ReportMissingCommand { get; }

        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public WorkerToolDetailsViewModel(
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

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(
                    () =>
                        OnPropertyChanged(
                            nameof(ThemeIcon)));

            BorrowCommand =
                new Command(
                    async () =>
                        await BorrowAsync(),
                    () =>
                        !IsBusy);

            ReturnCommand =
                new Command(
                    async () =>
                        await ReturnAsync(),
                    () =>
                        !IsBusy);

            TransferCommand =
                new Command(
                    async () =>
                        await TransferAsync(),
                    () =>
                        !IsBusy);

            RequestBorrowCommand =
                new Command(
                    async () =>
                        await RequestBorrowAsync(),
                    () =>
                        !IsBusy);

            EndDayCheckInCommand =
                new Command(
                    async () =>
                        await EndDayCheckInAsync(),
                    () =>
                        !IsBusy);

            ReportMissingCommand =
                new Command(
                    async () =>
                        await ReportMissingAsync(),
                    () =>
                        !IsBusy);

            ConfirmReceiptCommand =
                new Command(
                    async () =>
                        await ConfirmReceiptAsync(),
                    () =>
                        !IsBusy);

            DeclineCommand =
                new Command(
                    async () =>
                        await DeclineReceiptAsync(),
                    () =>
                        !IsBusy);

            GoBackCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(".."));

            ToggleThemeCommand =
                new Command(
                    () =>
                        _theme.Toggle());

            RefreshCommand =
                new Command(
                    async () =>
                        await LoadToolAsync());

            ViewHistoryCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(
                                $"//TransactionHistoryView" +
                                $"?toolId=" +
                                $"{Uri.EscapeDataString(ToolId)}" +
                                $"&viewMode=worker"));
        }

        // ─────────────────────────────────────────────────────────
        // LOAD TOOL
        // ─────────────────────────────────────────────────────────

        private async Task LoadToolAsync()
        {
            if (string.IsNullOrWhiteSpace(
                    ToolId))
            {
                return;
            }

            IsLoading =
                true;

            ToolNotFound =
                false;

            try
            {
                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            ToolId);

                if (tool == null)
                {
                    ToolNotFound =
                        true;

                    Tool =
                        null;

                    HasActiveMissingReport =
                        false;

                    return;
                }

                Tool =
                    tool;

                var lostReports =
                    await _firebase
                        .GetAllLostReportsRawAsync();

                HasActiveMissingReport =
                    lostReports.Any(r =>

                        string.Equals(
                            r.Report.ToolId,
                            tool.ToolId,
                            StringComparison.OrdinalIgnoreCase) &&

                        (
                            r.Report.Status ==
                                "Pending" ||

                            r.Report.Status ==
                                "Lost"
                        ));
            }
            catch (Exception ex)
            {
                await Shell.Current
                    .DisplayAlert(
                        "Error",
                        $"Could not load tool.\n" +
                        $"{ex.Message}",
                        "OK");
            }
            finally
            {
                IsLoading =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // ACTIVE PROJECT
        // ─────────────────────────────────────────────────────────

        private async Task<Project?>
            GetCurrentWorkerProjectAsync()
        {
            var user =
                _auth.CurrentUser;

            if (user == null ||
                string.IsNullOrWhiteSpace(
                    user.UniqueKey))
            {
                return null;
            }

            var project =
                await _firebase
                    .GetProjectForWorkerAsync(
                        user.UniqueKey);

            if (project == null ||
                string.IsNullOrWhiteSpace(
                    project.ProjectId))
            {
                return null;
            }

            var workerKeys =
                await _firebase
                    .GetProjectWorkerKeysAsync(
                        project.ProjectId);

            bool isAssigned =
                workerKeys.Any(key =>
                    string.Equals(
                        key?.Trim(),
                        user.UniqueKey.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            return isAssigned
                ? project
                : null;
        }

        // ─────────────────────────────────────────────────────────
        // TRANSACTION LOG
        // ─────────────────────────────────────────────────────────

        private async Task LogAsync(
            string action,
            string description,
            string condition)
        {
            if (Tool == null)
                return;

            var user =
                _auth.CurrentUser;

            if (user == null)
                return;

            await _firebase
                .LogTransactionAsync(
                    new TransactionLog
                    {
                        ToolId =
                            Tool.ToolId,

                        ToolName =
                            Tool.ToolName,

                        WorkerId =
                            user.UniqueKey,

                        WorkerName =
                            user.FullName,

                        ProjectId =
                            Tool.BorrowedProjectId ??
                            string.Empty,

                        ProjectName =
                            Tool.BorrowedProjectName ??
                            string.Empty,

                        PerformedById =
                            user.UniqueKey,

                        PerformedByName =
                            user.FullName,

                        Action =
                            action,

                        Description =
                            description,

                        Condition =
                            string.IsNullOrWhiteSpace(
                                condition)
                                ? "Good"
                                : condition,

                        Date =
                            DateTime.Now
                    });
        }

        // ─────────────────────────────────────────────────────────
        // REPORT MISSING
        // ─────────────────────────────────────────────────────────

        private async Task ReportMissingAsync()
        {
            if (Tool == null ||
                IsBusy ||
                !ShowReportMissing)
            {
                return;
            }

            var reason =
                await Shell.Current
                    .DisplayPromptAsync(
                        "Report Missing Equipment",
                        "Briefly explain why the equipment cannot be located:",
                        "Continue",
                        "Cancel",
                        placeholder:
                            "e.g. Unable to locate it in the storage area");

            if (string.IsNullOrWhiteSpace(
                    reason))
            {
                return;
            }

            reason =
                reason.Trim();

            bool confirm =
                await Shell.Current
                    .DisplayAlert(
                        "Submit Missing Report",
                        $"{Tool.ToolName} ({Tool.ToolId})\n\n" +
                        $"Reason: {reason}\n\n" +
                        "The equipment will remain assigned to you " +
                        "until the Project Engineer verifies the report.",
                        "Submit",
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
                {
                    return;
                }

                var report =
                    new LostReport
                    {
                        ToolId =
                            Tool.ToolId,

                        ToolName =
                            Tool.ToolName,

                        WorkerId =
                            user.UniqueKey,

                        WorkerName =
                            user.FullName,

                        ProjectId =
                            Tool.BorrowedProjectId ??
                            string.Empty,

                        ProjectName =
                            Tool.BorrowedProjectName ??
                            string.Empty,

                        MissingDescription =
                            reason,

                        Status =
                            "Pending",

                        ReportDate =
                            DateTime.Now
                    };

                var result =
                    await _firebase
                        .SubmitLostReportAsync(
                            report);

                if (result ==
                    "DUPLICATE")
                {
                    HasActiveMissingReport =
                        true;

                    await Shell.Current
                        .DisplayAlert(
                            "Report Already Exists",
                            "This equipment already has an active missing or lost report.",
                            "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(
                        result))
                {
                    await Shell.Current
                        .DisplayAlert(
                            "Error",
                            "Could not submit the missing equipment report.",
                            "OK");

                    return;
                }

                // IMPORTANT:
                // Tool stays Borrowed.
                // Worker remains accountable.
                // Project stays attached.

                await LogAsync(
                    "Missing Reported",
                    $"Worker reported the equipment missing. " +
                    $"Reason: {reason}",
                    Tool.Condition);

                HasActiveMissingReport =
                    true;

                await Shell.Current
                    .DisplayAlert(
                        "Missing Report Submitted",
                        $"{Tool.ToolName} ({Tool.ToolId}) " +
                        "is awaiting Project Engineer verification.\n\n" +
                        "You remain responsible for the equipment " +
                        "until the report is verified.",
                        "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current
                    .DisplayAlert(
                        "Error",
                        $"Could not submit the missing report.\n" +
                        $"{ex.Message}",
                        "OK");
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // BORROW
        // ─────────────────────────────────────────────────────────

        private async Task BorrowAsync()
        {
            if (Tool == null ||
                IsBusy)
            {
                return;
            }

            if (Tool.Status !=
                "Available")
            {
                return;
            }

            IsBusy =
                true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                    return;

                var activeProject =
                    await GetCurrentWorkerProjectAsync();

                if (activeProject == null)
                {
                    await Shell.Current
                        .DisplayAlert(
                            "No Active Project",
                            "You must be assigned to an active project before borrowing equipment.",
                            "OK");

                    return;
                }

                bool confirm =
                    await Shell.Current
                        .DisplayAlert(
                            "Borrow Equipment",
                            $"Borrow {Tool.ToolName} " +
                            $"({Tool.ToolId})?\n\n" +
                            $"Project: {activeProject.ProjectName}",
                            "Borrow",
                            "Cancel");

                if (!confirm)
                    return;

                string existingCondition =
                    string.IsNullOrWhiteSpace(
                        Tool.Condition)
                        ? "Good"
                        : Tool.Condition;

                Tool.Status =
                    "Borrowed";

                Tool.AssignedWorkerId =
                    user.UniqueKey;

                Tool.AssignedWorkerName =
                    user.FullName;

                Tool.BorrowedProjectId =
                    activeProject.ProjectId;

                Tool.BorrowedProjectName =
                    activeProject.ProjectName;

                Tool.Condition =
                    existingCondition;

                Tool.BorrowDate =
                    DateTime.Now;

                var success =
                    await _firebase
                        .UpdateToolAsync(
                            Tool);

                if (!success)
                    return;

                await LogAsync(
                    "Borrowed",
                    $"Equipment borrowed for {activeProject.ProjectName}.",
                    existingCondition);

                RefreshToolProperties();

                await Shell.Current
                    .DisplayAlert(
                        "Equipment Borrowed",
                        $"{Tool.ToolName} ({Tool.ToolId}) has been borrowed.",
                        "OK");
            }
            catch (Exception ex)
            {
                await LoadToolAsync();

                await Shell.Current
                    .DisplayAlert(
                        "Error",
                        $"Could not borrow equipment.\n{ex.Message}",
                        "OK");
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // RETURN
        // ─────────────────────────────────────────────────────────

        private async Task ReturnAsync()
        {
            if (Tool == null ||
                IsBusy ||
                !ShowReturn)
            {
                return;
            }

            bool confirm =
                await Shell.Current
                    .DisplayAlert(
                        "Return Equipment",
                        $"Return {Tool.ToolName} ({Tool.ToolId})?\n\n" +
                        "Please bring the equipment to the Project Engineer " +
                        "for physical inspection.\n\n" +
                        "You remain responsible until the return is approved.",
                        "Submit Return",
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

                var request =
                    new ReturnRequest
                    {
                        ToolId =
                            Tool.ToolId,

                        ToolName =
                            Tool.ToolName,

                        WorkerId =
                            user.UniqueKey,

                        WorkerName =
                            user.FullName,

                        ProjectId =
                            Tool.BorrowedProjectId ??
                            string.Empty,

                        ProjectName =
                            Tool.BorrowedProjectName ??
                            string.Empty,

                        ReportedCondition =
                            string.Empty,

                        VerifiedCondition =
                            string.Empty,

                        Notes =
                            string.Empty,

                        Status =
                            "Pending",

                        RequestDate =
                            DateTime.Now
                    };

                var requestKey =
                    await _firebase
                        .CreateReturnRequestAsync(
                            request);

                if (string.IsNullOrWhiteSpace(
                        requestKey))
                {
                    return;
                }

                Tool.Status =
                    "PendingReturn";

                var updated =
                    await _firebase
                        .UpdateToolAsync(
                            Tool);

                if (!updated)
                {
                    request.Status =
                        "Rejected";

                    request.Notes =
                        "Return request cancelled because the equipment status could not be updated.";

                    request.ReviewedDate =
                        DateTime.Now;

                    await _firebase
                        .UpdateReturnRequestAsync(
                            requestKey,
                            request);

                    Tool.Status =
                        "Borrowed";

                    return;
                }

                await LogAsync(
                    "Return Requested",
                    "Equipment submitted for return and awaiting Project Engineer physical inspection.",
                    Tool.Condition);

                RefreshToolProperties();

                await Shell.Current
                    .DisplayAlert(
                        "Return Submitted",
                        $"{Tool.ToolName} ({Tool.ToolId}) is awaiting Project Engineer inspection.",
                        "OK");
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // TRANSFER
        // ─────────────────────────────────────────────────────────

        private async Task TransferAsync()
        {
            if (Tool == null ||
                IsBusy ||
                !ShowTransfer)
            {
                return;
            }

            IsBusy =
                true;

            try
            {
                var allUsers =
                    await _firebase
                        .GetAllUsersAsync();

                var workers =
                    allUsers
                        .Where(user =>
                            !string.Equals(
                                user.UniqueKey,
                                CurrentUserKey,
                                StringComparison.OrdinalIgnoreCase) &&

                            user.Role ==
                                "Worker" &&

                            user.AccountStatus ==
                                "Approved")
                        .ToList();

                if (workers.Count == 0)
                    return;

                var selected =
                    await Shell.Current
                        .DisplayActionSheet(
                            "Transfer To",
                            "Cancel",
                            null,
                            workers
                                .Select(worker =>
                                    worker.FullName)
                                .ToArray());

                if (string.IsNullOrWhiteSpace(
                        selected) ||
                    selected ==
                        "Cancel")
                {
                    return;
                }

                var target =
                    workers
                        .FirstOrDefault(worker =>
                            worker.FullName ==
                            selected);

                if (target == null)
                    return;

                var user =
                    _auth.CurrentUser;

                if (user == null)
                    return;

                var request =
                    new TransferRequest
                    {
                        ToolId =
                            Tool.ToolId,

                        ToolName =
                            Tool.ToolName,

                        FromWorkerId =
                            user.UniqueKey,

                        FromWorkerName =
                            user.FullName,

                        ToWorkerId =
                            target.UniqueKey,

                        ToWorkerName =
                            target.FullName,

                        ProjectId =
                            Tool.BorrowedProjectId ??
                            string.Empty,

                        ProjectName =
                            Tool.BorrowedProjectName ??
                            string.Empty,

                        Condition =
                            Tool.Condition,

                        Status =
                            "Pending",

                        RequestDate =
                            DateTime.Now
                    };

                var key =
                    await _firebase
                        .CreateTransferRequestAsync(
                            request);

                if (string.IsNullOrWhiteSpace(
                        key))
                {
                    return;
                }

                await Shell.Current
                    .DisplayAlert(
                        "Transfer Request Sent",
                        $"Transfer request sent to {target.FullName}.",
                        "OK");
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // REQUEST BORROW
        // ─────────────────────────────────────────────────────────

        private async Task RequestBorrowAsync()
        {
            if (Tool == null ||
                IsBusy ||
                Tool.Status != "Borrowed" ||
                IsAssignedToMe)
            {
                return;
            }

            IsBusy =
                true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                    return;

                var activeProject =
                    await GetCurrentWorkerProjectAsync();

                if (activeProject == null)
                    return;

                bool confirm =
                    await Shell.Current
                        .DisplayAlert(
                            "Request Borrow",
                            $"Send a borrow request for {Tool.ToolName} ({Tool.ToolId})?",
                            "Send Request",
                            "Cancel");

                if (!confirm)
                    return;

                var request =
                    new BorrowRequest
                    {
                        ToolId =
                            Tool.ToolId,

                        ToolName =
                            Tool.ToolName,

                        RequesterId =
                            user.UniqueKey,

                        RequesterName =
                            user.FullName,

                        OwnerId =
                            Tool.AssignedWorkerId,

                        OwnerName =
                            Tool.AssignedWorkerName,

                        Status =
                            "Pending",

                        RequestDate =
                            DateTime.Now
                    };

                var result =
                    await _firebase
                        .CreateBorrowRequestAsync(
                            request);

                if (result ==
                    "DUPLICATE")
                {
                    await Shell.Current
                        .DisplayAlert(
                            "Request Already Pending",
                            "You already have a pending request for this tool.",
                            "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(
                        result))
                {
                    return;
                }

                await Shell.Current
                    .DisplayAlert(
                        "Request Sent",
                        "Your borrow request was sent.",
                        "OK");
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // CONFIRM RECEIPT
        // ─────────────────────────────────────────────────────────

        private async Task ConfirmReceiptAsync()
        {
            if (Tool == null ||
                IsBusy)
            {
                return;
            }

            var user =
                _auth.CurrentUser;

            if (user == null)
                return;

            if (!ShowConfirmReceipt)
            {
                await LoadToolAsync();
                return;
            }

            bool confirm =
                await Shell.Current
                    .DisplayAlert(
                        "Confirm Receipt",
                        $"Confirm that you physically received " +
                        $"{Tool.ToolName} ({Tool.ToolId})?",
                        "Confirm",
                        "Cancel");

            if (!confirm)
                return;

            IsBusy =
                true;

            try
            {
                string existingCondition =
                    string.IsNullOrWhiteSpace(
                        Tool.Condition)
                        ? "Good"
                        : Tool.Condition;

                Tool.Status =
                    "Borrowed";

                Tool.AssignedWorkerId =
                    user.UniqueKey;

                Tool.AssignedWorkerName =
                    user.FullName;

                Tool.Condition =
                    existingCondition;

                Tool.BorrowDate =
                    DateTime.Now;

                Tool.PreAssignedWorkerId =
                    string.Empty;

                Tool.PreAssignedWorkerName =
                    string.Empty;

                var updated =
                    await _firebase
                        .UpdateToolAsync(
                            Tool);

                if (!updated)
                    return;

                await LogAsync(
                    "Borrowed",
                    "Worker physically confirmed receipt of the assigned equipment.",
                    existingCondition);

                RefreshToolProperties();

                await Shell.Current
                    .DisplayAlert(
                        "Receipt Confirmed",
                        $"You have confirmed receipt of {Tool.ToolName}.",
                        "OK");
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // END-DAY CHECK-IN
        // ─────────────────────────────────────────────────────────

        private async Task EndDayCheckInAsync()
        {
            if (Tool == null ||
                IsBusy ||
                !ShowEndDayCheckIn)
            {
                return;
            }

            string[] locations =
            {
                "Site Storage",
                "Tool Room",
                "Warehouse",
                "Worker Area",
                "Other"
            };

            var selectedLocation =
                await Shell.Current
                    .DisplayActionSheet(
                        "End Day Check-In",
                        "Cancel",
                        null,
                        locations);

            if (string.IsNullOrWhiteSpace(
                    selectedLocation) ||
                selectedLocation ==
                    "Cancel")
            {
                return;
            }

            if (selectedLocation ==
                "Other")
            {
                selectedLocation =
                    await Shell.Current
                        .DisplayPromptAsync(
                            "Storage Location",
                            "Enter where the equipment will be stored:",
                            "Continue",
                            "Cancel");

                if (string.IsNullOrWhiteSpace(
                        selectedLocation))
                {
                    return;
                }
            }

            bool confirm =
                await Shell.Current
                    .DisplayAlert(
                        "Confirm End-Day Check-In",
                        $"{Tool.ToolName} ({Tool.ToolId})\n\n" +
                        $"Location: {selectedLocation}",
                        "Check In",
                        "Cancel");

            if (!confirm)
                return;

            IsBusy =
                true;

            try
            {
                Tool.LastCheckInLocation =
                    selectedLocation.Trim();

                Tool.LastCheckInDate =
                    DateTime.Now;

                Tool.IsCheckInPending =
                    true;

                Tool.LastCheckInVerifiedById =
                    string.Empty;

                Tool.LastCheckInVerifiedByName =
                    string.Empty;

                var updated =
                    await _firebase
                        .UpdateToolAsync(
                            Tool);

                if (!updated)
                    return;

                await LogAsync(
                    "End Day Check-In",
                    $"Equipment reported at {selectedLocation} and is awaiting Project Engineer verification.",
                    Tool.Condition);

                RefreshToolProperties();

                await Shell.Current
                    .DisplayAlert(
                        "Check-In Submitted",
                        "End-day check-in submitted.",
                        "OK");
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // DECLINE RECEIPT
        // ─────────────────────────────────────────────────────────

        private async Task DeclineReceiptAsync()
        {
            if (Tool == null ||
                IsBusy)
            {
                return;
            }

            bool confirm =
                await Shell.Current
                    .DisplayAlert(
                        "Decline Assignment",
                        $"Decline {Tool.ToolName} ({Tool.ToolId})?",
                        "Decline",
                        "Cancel");

            if (!confirm)
                return;

            IsBusy =
                true;

            try
            {
                Tool.PreAssignedWorkerId =
                    string.Empty;

                Tool.PreAssignedWorkerName =
                    string.Empty;

                var updated =
                    await _firebase
                        .UpdateToolAsync(
                            Tool);

                if (!updated)
                    return;

                await LogAsync(
                    "Declined",
                    "Declined equipment assignment.",
                    Tool.Condition);

                RefreshToolProperties();

                await Shell.Current
                    .DisplayAlert(
                        "Declined",
                        "Equipment assignment declined.",
                        "OK");
            }
            finally
            {
                IsBusy =
                    false;
            }
        }
    }
}