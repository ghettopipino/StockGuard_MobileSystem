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
                SetProperty(ref _toolId, value);

                if (!string.IsNullOrEmpty(value))
                {
                    MainThread.BeginInvokeOnMainThread(
                        async () => await LoadToolAsync());
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
                SetProperty(ref _tool, value);

                RefreshToolProperties();
            }
        }

        private void RefreshToolProperties()
        {
            OnPropertyChanged(nameof(ToolName));
            OnPropertyChanged(nameof(ToolIdDisplay));
            OnPropertyChanged(nameof(ToolIcon));

            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusIcon));

            OnPropertyChanged(nameof(AssignedWorkerName));
            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(AssignedByName));
            OnPropertyChanged(nameof(BorrowDateDisplay));
            OnPropertyChanged(nameof(ConditionText));

            OnPropertyChanged(nameof(IsAssignedToMe));

            OnPropertyChanged(nameof(ShowBorrow));
            OnPropertyChanged(nameof(ShowReturn));
            OnPropertyChanged(nameof(ShowPendingReturn));
            OnPropertyChanged(nameof(ShowTransfer));
            OnPropertyChanged(nameof(ShowReportDamage));
            OnPropertyChanged(nameof(ShowRequestBorrow));
            OnPropertyChanged(nameof(ShowEndDayCheckIn));
            OnPropertyChanged(nameof(ShowPendingCheckIn));
            OnPropertyChanged(nameof(CheckInLocation));
            OnPropertyChanged(nameof(CheckInDateDisplay));

            OnPropertyChanged(nameof(ShowConfirmReceipt));
            OnPropertyChanged(nameof(ShowDeclineReceipt));
        }

        // ─────────────────────────────────────────────────────────
        // DISPLAY PROPERTIES
        // ─────────────────────────────────────────────────────────

        public string ToolName =>
            Tool?.ToolName ?? "Loading...";

        public string ToolIdDisplay =>
            Tool?.ToolId ?? string.Empty;

        public string ToolIcon =>
            Tool?.ToolIcon ?? "🔧";

        public string StatusText =>
            Tool?.Status ?? string.Empty;

        public string StatusColor =>
            Tool?.StatusColor ?? "#6b7280";

        public string StatusIcon =>
            Tool?.StatusIcon ?? "❓";

        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

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
            Tool?.BorrowDate.HasValue == true
                ? Tool.BorrowDate.Value
                    .ToString("MMM d, yyyy h:mm tt")
                : "— Not borrowed —";

        public string ConditionText =>
            string.IsNullOrWhiteSpace(Tool?.Condition)
                ? "Good"
                : Tool.Condition;

        // ─────────────────────────────────────────────────────────
        // CURRENT USER / VISIBILITY
        // ─────────────────────────────────────────────────────────

        private string CurrentUserKey =>
            _auth.CurrentUser?.UniqueKey
            ?? string.Empty;

        public bool IsAssignedToMe =>
            Tool != null &&
            !string.IsNullOrWhiteSpace(
                Tool.AssignedWorkerId) &&
            Tool.AssignedWorkerId == CurrentUserKey;

        // Available equipment
        public bool ShowBorrow =>
            Tool != null &&
            Tool.Status == "Available";

        // Worker currently has equipment
        public bool ShowReturn =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status == "Borrowed";

        // Worker already submitted return
        public bool ShowPendingReturn =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status == "PendingReturn";

        public bool ShowEndDayCheckIn =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status == "Borrowed" &&
            !Tool.IsCheckInPending;

        public bool ShowPendingCheckIn =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status == "Borrowed" &&
            Tool.IsCheckInPending;

        public string CheckInLocation =>
            string.IsNullOrWhiteSpace(Tool?.LastCheckInLocation)
                ? "—"
                : Tool.LastCheckInLocation;

        public string CheckInDateDisplay =>
            Tool?.LastCheckInDate.HasValue == true
                ? Tool.LastCheckInDate.Value.ToString("MMM d, yyyy h:mm tt")
                : "—";

        public bool ShowTransfer =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status == "Borrowed";

        public bool ShowReportDamage =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status == "Borrowed";

        public bool ShowRequestBorrow =>
            Tool != null &&
            Tool.Status == "Borrowed" &&
            !IsAssignedToMe;

        public bool ShowConfirmReceipt =>
            Tool != null &&
            Tool.PreAssignedWorkerId ==
                CurrentUserKey &&
            Tool.Status == "Available";

        public bool ShowDeclineReceipt =>
            ShowConfirmReceipt;

        // ─────────────────────────────────────────────────────────
        // LOADING
        // ─────────────────────────────────────────────────────────

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);

                OnPropertyChanged(
                    nameof(IsNotLoading));
            }
        }

        public bool IsNotLoading =>
            !IsLoading;

        private bool _toolNotFound;

        public bool ToolNotFound
        {
            get => _toolNotFound;
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
        public ICommand ReportDamageCommand { get; }
        public ICommand RequestBorrowCommand { get; }

        public ICommand ConfirmReceiptCommand { get; }
        public ICommand DeclineCommand { get; }

        public ICommand GoBackCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewHistoryCommand { get; }
        public ICommand EndDayCheckInCommand { get; }

        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public WorkerToolDetailsViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(
                    () =>
                        OnPropertyChanged(
                            nameof(ThemeIcon)));

            BorrowCommand =
                new Command(
                    async () =>
                        await BorrowAsync(),
                    () => !IsBusy);

            ReturnCommand =
                new Command(
                    async () =>
                        await ReturnAsync(),
                    () => !IsBusy);

            TransferCommand =
                new Command(
                    async () =>
                        await TransferAsync(),
                    () => !IsBusy);

            ReportDamageCommand =
                new Command(
                    async () =>
                        await ReportDamageAsync(),
                    () => !IsBusy);

            RequestBorrowCommand =
                new Command(
                    async () =>
                        await RequestBorrowAsync(),
                    () => !IsBusy);

            EndDayCheckInCommand =
                new Command(
                    async () => await EndDayCheckInAsync(),
                    () => !IsBusy);

            ConfirmReceiptCommand =
                new Command(
                    async () =>
                        await ConfirmReceiptAsync(),
                    () => !IsBusy);

            DeclineCommand =
                new Command(
                    async () =>
                        await DeclineReceiptAsync(),
                    () => !IsBusy);

            GoBackCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(".."));

            ToggleThemeCommand =
                new Command(
                    () => _theme.Toggle());

            RefreshCommand =
                new Command(
                    async () =>
                        await LoadToolAsync());

            ViewHistoryCommand =
                new Command(
                    async () =>
                        await Shell.Current.GoToAsync(
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
            if (string.IsNullOrEmpty(ToolId))
                return;

            IsLoading = true;
            ToolNotFound = false;

            try
            {
                var tool =
                    await _firebase
                        .GetToolByIdAsync(ToolId);

                if (tool == null)
                {
                    ToolNotFound = true;
                    Tool = null;

                    return;
                }

                Tool = tool;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not load tool.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // TRANSACTION LOG
        // ─────────────────────────────────────────────────────────

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

            await _firebase.LogTransactionAsync(
                new TransactionLog
                {
                    // ── EQUIPMENT ────────────────────────────────

                    ToolId =
                        Tool.ToolId,

                    ToolName =
                        Tool.ToolName,

                    // ── RESPONSIBLE WORKER ───────────────────────

                    WorkerId =
                        user.UniqueKey,

                    WorkerName =
                        user.FullName,

                    // ── PROJECT ──────────────────────────────────

                    ProjectId =
                        Tool.BorrowedProjectId ?? string.Empty,

                    ProjectName =
                        Tool.BorrowedProjectName ?? string.Empty,

                    // ── PERSON WHO PERFORMED ACTION ──────────────
                    //
                    // This ViewModel is worker-side, so the
                    // currently logged-in worker performed the
                    // action.

                    PerformedById =
                        user.UniqueKey,

                    PerformedByName =
                        user.FullName,

                    // ── ACTIVITY ─────────────────────────────────

                    Action =
                        action,

                    Description =
                        description,

                    Condition =
                        string.IsNullOrWhiteSpace(condition)
                            ? "Good"
                            : condition,

                    Date =
                        DateTime.Now
                });
        }

        // ─────────────────────────────────────────────────────────
        // BORROW
        // ─────────────────────────────────────────────────────────

        private async Task BorrowAsync()
        {
            if (Tool == null || IsBusy)
                return;

            if (Tool.Status != "Available")
                return;

            IsBusy = true;

            try
            {
                var condition =
                    await Shell.Current
                        .DisplayActionSheet(
                            "Tool Condition",
                            "Cancel",
                            null,
                            "Good",
                            "Minor Damage",
                            "Major Damage");

                if (string.IsNullOrWhiteSpace(condition) ||
                    condition == "Cancel")
                {
                    return;
                }

                var user = _auth.CurrentUser;

                if (user == null)
                    return;

                Tool.Status =
                    "Borrowed";

                Tool.AssignedWorkerId =
                    user.UniqueKey;

                Tool.AssignedWorkerName =
                    user.FullName;

                Tool.Condition =
                    condition;

                Tool.BorrowDate =
                    DateTime.Now;

                var success =
                    await _firebase
                        .UpdateToolAsync(Tool);

                if (!success)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not borrow tool.",
                        "OK");

                    return;
                }

                await LogAsync(
                    "Borrowed",
                    $"Equipment borrowed in " +
                    $"{condition} condition.",
                    condition);

                RefreshToolProperties();

                await Shell.Current.DisplayAlert(
                    "Tool Borrowed",
                    $"{Tool.ToolName} " +
                    $"({Tool.ToolId}) has been borrowed.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not borrow tool.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // RETURN
        // ─────────────────────────────────────────────────────────

        private async Task ReturnAsync()
        {
            if (Tool == null || IsBusy)
                return;

            if (!IsAssignedToMe ||
                Tool.Status != "Borrowed")
            {
                return;
            }

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Return Tool",
                    $"Return {Tool.ToolName} " +
                    $"({Tool.ToolId})?\n\n" +
                    "Please bring the equipment to the " +
                    "Project Engineer for inspection. " +
                    "You remain responsible for it until " +
                    "the return is approved.",
                    "Submit Return",
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
                        "Your user session could not be found.",
                        "OK");

                    return;
                }

                // Save current condition only for the
                // transaction history.
                //
                // The worker does NOT determine the
                // official return condition.
                string currentCondition =
                    Tool.Condition;

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
                            Tool.BorrowedProjectId
                            ?? string.Empty,

                        ProjectName =
                            Tool.BorrowedProjectName
                            ?? string.Empty,

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

                // Create request first.
                var requestKey =
                    await _firebase
                        .CreateReturnRequestAsync(
                            request);

                if (string.IsNullOrEmpty(requestKey))
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not submit the return request.",
                        "OK");

                    return;
                }

                // Worker remains accountable until
                // the Project Engineer approves.
                Tool.Status =
                    "PendingReturn";

                var updated =
                    await _firebase
                        .UpdateToolAsync(Tool);

                if (!updated)
                {
                    // Cancel the request if the Tool
                    // could not enter PendingReturn.
                    request.Status =
                        "Rejected";

                    request.Notes =
                        "Return request cancelled because " +
                        "the equipment status could not be updated.";

                    await _firebase
                        .UpdateReturnRequestAsync(
                            requestKey,
                            request);

                    // Restore local state.
                    Tool.Status =
                        "Borrowed";

                    RefreshToolProperties();

                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not update the equipment status.",
                        "OK");

                    return;
                }

                await LogAsync(
                    "Return Requested",
                    "Equipment submitted for return and " +
                    "awaiting Project Engineer inspection.",
                    currentCondition);

                // IMPORTANT:
                // Tool was mutated directly.
                // Explicitly refresh every dependent UI property.
                RefreshToolProperties();

                await Shell.Current.DisplayAlert(
                    "Return Submitted",
                    $"{Tool.ToolName} ({Tool.ToolId}) " +
                    $"is pending return inspection.\n\n" +
                    "Please bring the equipment to the " +
                    "Project Engineer.",
                    "OK");
            }
            catch (Exception ex)
            {
                // Reload from Firebase if something fails
                // so the UI reflects the actual saved state.
                await LoadToolAsync();

                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not submit return request.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // TRANSFER
        // ─────────────────────────────────────────────────────────

        private async Task TransferAsync()
        {
            if (Tool == null || IsBusy)
                return;

            if (!IsAssignedToMe ||
                Tool.Status != "Borrowed")
            {
                return;
            }

            IsBusy = true;

            try
            {
                var allUsers =
                    await _firebase
                        .GetAllUsersAsync();

                var workers =
                    allUsers
                        .Where(u =>
                            u.UniqueKey !=
                                CurrentUserKey &&
                            u.Role == "Worker" &&
                            u.AccountStatus ==
                                "Approved")
                        .ToList();

                if (workers.Count == 0)
                {
                    await Shell.Current.DisplayAlert(
                        "No Workers",
                        "No other approved workers are available.",
                        "OK");

                    return;
                }

                var selected =
                    await Shell.Current
                        .DisplayActionSheet(
                            "Transfer To",
                            "Cancel",
                            null,
                            workers
                                .Select(w => w.FullName)
                                .ToArray());

                if (string.IsNullOrWhiteSpace(selected) ||
                    selected == "Cancel")
                {
                    return;
                }

                var target =
                    workers.FirstOrDefault(w =>
                        w.FullName == selected);

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

                        Status =
                            "Pending",

                        RequestDate =
                            DateTime.Now
                    };

                var key =
                    await _firebase
                        .CreateTransferRequestAsync(
                            request);

                if (string.IsNullOrEmpty(key))
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not send transfer request.",
                        "OK");

                    return;
                }

                await Shell.Current.DisplayAlert(
                    "Transfer Request Sent",
                    $"Transfer request sent to " +
                    $"{target.FullName}.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not create transfer request.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // DAMAGE
        // ─────────────────────────────────────────────────────────

        private async Task ReportDamageAsync()
        {
            if (Tool == null || IsBusy)
                return;

            // Only the worker currently holding the equipment
            // can report damage.
            if (!IsAssignedToMe ||
                Tool.Status != "Borrowed")
            {
                return;
            }

            // Save these BEFORE changing the tool status.
            string projectId =
                Tool.BorrowedProjectId ?? string.Empty;

            string projectName =
                Tool.BorrowedProjectName ?? string.Empty;

            string projectEngineerId =
                Tool.AssignedById ?? string.Empty;

            string projectEngineerName =
                Tool.AssignedByName ?? string.Empty;

            // ── DAMAGE SEVERITY ───────────────────────────────────

            var severity =
                await Shell.Current.DisplayActionSheet(
                    "Damage Severity",
                    "Cancel",
                    null,
                    "Minor Damage",
                    "Major Damage");

            if (string.IsNullOrWhiteSpace(severity) ||
                severity == "Cancel")
            {
                return;
            }

            // ── DAMAGE DESCRIPTION ────────────────────────────────

            var description =
                await Shell.Current.DisplayPromptAsync(
                    "Damage Description",
                    "Briefly describe what happened to the equipment:",
                    "Submit",
                    "Cancel",
                    placeholder: "e.g. Power cable was damaged during use");

            if (string.IsNullOrWhiteSpace(description))
                return;

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Submit Damage Report",
                    $"{Tool.ToolName} ({Tool.ToolId})\n\n" +
                    $"Project: {projectName}\n" +
                    $"Severity: {severity}\n\n" +
                    "Submit this damage report?",
                    "Submit",
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
                        "Your user session could not be found.",
                        "OK");

                    return;
                }

                // ── CREATE DAMAGE REPORT ───────────────────────────

                var report =
                    new DamageReport
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
                            projectId,

                        ProjectName =
                            projectName,

                        ProjectEngineerId =
                            projectEngineerId,

                        ProjectEngineerName =
                            projectEngineerName,

                        Description =
                            description.Trim(),

                        Severity =
                            severity,

                        Status =
                            "Pending",

                        ReportDate =
                            DateTime.Now
                    };

                var reportKey =
                    await _firebase.SubmitDamageReportAsync(
                        report);

                if (string.IsNullOrEmpty(reportKey))
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not submit the damage report.",
                        "OK");

                    return;
                }

                // ── UPDATE EQUIPMENT ───────────────────────────────
                //
                // IMPORTANT:
                // Do NOT clear Worker/Project/PE here.
                //
                // We still need to know who was responsible
                // when the damage occurred.

                Tool.Status =
                    "Damaged";

                Tool.Condition =
                    severity;

                var updated =
                    await _firebase.UpdateToolAsync(Tool);

                if (!updated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The damage report was submitted, but the " +
                        "equipment status could not be updated.",
                        "OK");

                    return;
                }

                // ── TRANSACTION HISTORY ────────────────────────────

                await LogAsync(
                    "Damage Reported",
                    $"Damage reported: {severity} — {description.Trim()}",
                    severity);

                RefreshToolProperties();

                await Shell.Current.DisplayAlert(
                    "Damage Reported",
                    $"{Tool.ToolName} ({Tool.ToolId}) has been marked as damaged.\n\n" +
                    $"Project: {projectName}\n" +
                    $"Project Engineer: {projectEngineerName}\n" +
                    $"Severity: {severity}",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not submit damage report.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        // ─────────────────────────────────────────────────────────
        // REQUEST BORROW
        // ─────────────────────────────────────────────────────────

        private async Task RequestBorrowAsync()
        {
            if (Tool == null || IsBusy)
                return;

            IsBusy = true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                    return;

                bool confirm =
                    await Shell.Current.DisplayAlert(
                        "Request Borrow",
                        $"Send a borrow request for " +
                        $"{Tool.ToolName} ({Tool.ToolId}) " +
                        $"to {Tool.AssignedWorkerName}?",
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

                var key =
                    await _firebase
                        .CreateBorrowRequestAsync(
                            request);

                if (string.IsNullOrEmpty(key))
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not send borrow request.",
                        "OK");

                    return;
                }

                await Shell.Current.DisplayAlert(
                    "Request Sent",
                    $"Your borrow request was sent to " +
                    $"{Tool.AssignedWorkerName}.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not send request.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // CONFIRM RECEIPT
        // ─────────────────────────────────────────────────────────

        private async Task ConfirmReceiptAsync()
        {
            if (Tool == null || IsBusy)
                return;

            IsBusy = true;

            try
            {
                var condition =
                    await Shell.Current
                        .DisplayActionSheet(
                            "Tool Condition",
                            "Cancel",
                            null,
                            "Good",
                            "Minor Damage",
                            "Major Damage");

                if (string.IsNullOrWhiteSpace(condition) ||
                    condition == "Cancel")
                {
                    return;
                }

                var user =
                    _auth.CurrentUser;

                if (user == null)
                    return;

                Tool.Status =
                    "Borrowed";

                Tool.AssignedWorkerId =
                    user.UniqueKey;

                Tool.AssignedWorkerName =
                    user.FullName;

                Tool.Condition =
                    condition;

                Tool.BorrowDate =
                    DateTime.Now;

                Tool.PreAssignedWorkerId =
                    string.Empty;

                Tool.PreAssignedWorkerName =
                    string.Empty;

                var updated =
                    await _firebase
                        .UpdateToolAsync(Tool);

                if (!updated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not confirm receipt.",
                        "OK");

                    return;
                }

                await LogAsync(
                    "Borrowed",
                    $"Confirmed receipt in " +
                    $"{condition} condition.",
                    condition);

                RefreshToolProperties();

                await Shell.Current.DisplayAlert(
                    "Receipt Confirmed",
                    $"You have confirmed receipt of " +
                    $"{Tool.ToolName} ({Tool.ToolId}).",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not confirm receipt.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // ENDDAY CHECKIN
        // ─────────────────────────────────────────────────────────

        private async Task EndDayCheckInAsync()
        {
            if (Tool == null || IsBusy)
                return;

            if (!IsAssignedToMe ||
                Tool.Status != "Borrowed")
            {
                return;
            }

            if (Tool.IsCheckInPending)
            {
                await Shell.Current.DisplayAlert(
                    "Check-In Pending",
                    "This equipment already has an end-day check-in waiting for verification.",
                    "OK");

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
                await Shell.Current.DisplayActionSheet(
                    "End Day Check-In",
                    "Cancel",
                    null,
                    locations);

            if (string.IsNullOrWhiteSpace(selectedLocation) ||
                selectedLocation == "Cancel")
            {
                return;
            }

            if (selectedLocation == "Other")
            {
                selectedLocation =
                    await Shell.Current.DisplayPromptAsync(
                        "Storage Location",
                        "Enter where the equipment will be stored:",
                        "Continue",
                        "Cancel",
                        placeholder:
                            "e.g. Building A - Tool Storage");

                if (string.IsNullOrWhiteSpace(selectedLocation))
                    return;

                selectedLocation =
                    selectedLocation.Trim();
            }

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Confirm End Day",
                    $"{Tool.ToolName} ({Tool.ToolId})\n\n" +
                    $"Store at: {selectedLocation}\n\n" +
                    "The equipment will remain assigned to you. " +
                    "The Project Engineer must verify the check-in.",
                    "Check In",
                    "Cancel");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                    return;

                Tool.LastCheckInLocation =
                    selectedLocation;

                Tool.LastCheckInDate =
                    DateTime.Now;

                Tool.IsCheckInPending =
                    true;

                Tool.LastCheckInVerifiedById =
                    string.Empty;

                Tool.LastCheckInVerifiedByName =
                    string.Empty;

                // IMPORTANT:
                // Status remains Borrowed.
                // Worker/project assignment stays unchanged.

                var updated =
                    await _firebase.UpdateToolAsync(Tool);

                if (!updated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not save the end-day check-in.",
                        "OK");

                    return;
                }

                await LogAsync(
                    "End Day Check-In",
                    $"Equipment checked in at {selectedLocation} " +
                    $"and is awaiting PE verification.",
                    Tool.Condition);

                RefreshToolProperties();

                await Shell.Current.DisplayAlert(
                    "Check-In Submitted",
                    $"{Tool.ToolName} ({Tool.ToolId}) has been checked in for the day.\n\n" +
                    $"Location: {selectedLocation}\n" +
                    "Status remains Borrowed until the equipment is formally returned.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not complete end-day check-in.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // DECLINE RECEIPT
        // ─────────────────────────────────────────────────────────

        private async Task DeclineReceiptAsync()
        {
            if (Tool == null || IsBusy)
                return;

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Decline Assignment",
                    $"Decline {Tool.ToolName} " +
                    $"({Tool.ToolId})?",
                    "Decline",
                    "Cancel");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                var toolName =
                    Tool.ToolName;

                var toolId =
                    Tool.ToolId;

                Tool.PreAssignedWorkerId =
                    string.Empty;

                Tool.PreAssignedWorkerName =
                    string.Empty;

                var updated =
                    await _firebase
                        .UpdateToolAsync(Tool);

                if (!updated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not decline assignment.",
                        "OK");

                    return;
                }

                await LogAsync(
                    "Declined",
                    "Declined equipment assignment.",
                    Tool.Condition);

                RefreshToolProperties();

                await Shell.Current.DisplayAlert(
                    "Declined",
                    $"You declined {toolName} ({toolId}).",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not decline assignment.\n" +
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