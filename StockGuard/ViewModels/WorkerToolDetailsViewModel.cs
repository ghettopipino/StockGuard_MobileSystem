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

                if (!string.IsNullOrWhiteSpace(value))
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
            string.IsNullOrWhiteSpace(
                Tool?.Condition)
                ? "Good"
                : Tool.Condition;

        // ─────────────────────────────────────────────────────────
        // CURRENT USER
        // ─────────────────────────────────────────────────────────

        private string CurrentUserKey =>
            _auth.CurrentUser?.UniqueKey
            ?? string.Empty;

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
            Tool.Status == "Available";

        public bool ShowReturn =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status == "Borrowed";

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

        public bool ShowTransfer =>
            Tool != null &&
            IsAssignedToMe &&
            Tool.Status == "Borrowed";


        public bool ShowRequestBorrow =>
            Tool != null &&
            Tool.Status == "Borrowed" &&
            !IsAssignedToMe;

        public bool ShowConfirmReceipt =>
            Tool != null &&
            string.Equals(
                Tool.PreAssignedWorkerId?.Trim(),
                CurrentUserKey.Trim(),
                StringComparison.OrdinalIgnoreCase) &&
            Tool.Status == "Available";

        public bool ShowDeclineReceipt =>
            ShowConfirmReceipt;

        // ─────────────────────────────────────────────────────────
        // CHECK-IN DISPLAY
        // ─────────────────────────────────────────────────────────

        public string CheckInLocation =>
            string.IsNullOrWhiteSpace(
                Tool?.LastCheckInLocation)
                ? "—"
                : Tool.LastCheckInLocation;

        public string CheckInDateDisplay =>
            Tool?.LastCheckInDate.HasValue == true
                ? Tool.LastCheckInDate.Value
                    .ToString("MMM d, yyyy h:mm tt")
                : "—";

        // ─────────────────────────────────────────────────────────
        // LOADING
        // ─────────────────────────────────────────────────────────

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;

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

          
            RequestBorrowCommand =
                new Command(
                    async () =>
                        await RequestBorrowAsync(),
                    () => !IsBusy);

            EndDayCheckInCommand =
                new Command(
                    async () =>
                        await EndDayCheckInAsync(),
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
            if (string.IsNullOrWhiteSpace(ToolId))
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
        // ACTIVE PROJECT VALIDATION
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

            await _firebase.LogTransactionAsync(
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
                        Tool.BorrowedProjectId
                        ?? string.Empty,

                    ProjectName =
                        Tool.BorrowedProjectName
                        ?? string.Empty,

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
        // WORKER DAMAGE INFORMATION
        //
        // Worker cannot evaluate damage.
        // PE evaluates condition during:
        //
        // 1. Return inspection
        // 2. End-day check-in verification
        // ─────────────────────────────────────────────────────────

        private async Task
            ShowDamageEvaluationInfoAsync()
        {
            await Shell.Current.DisplayAlert(
                "Project Engineer Inspection Required",
                "Equipment damage must be evaluated by " +
                "the Project Engineer.\n\n" +
                "Submit a Return or End-Day Check-In " +
                "so the equipment can be physically inspected.",
                "OK");
        }

        // ─────────────────────────────────────────────────────────
        // BORROW
        //
        // Worker DOES NOT choose Minor / Major.
        //
        // Existing equipment condition is preserved.
        // Damage classification belongs to PE.
        // ─────────────────────────────────────────────────────────

        private async Task BorrowAsync()
        {
            if (Tool == null ||
                IsBusy)
            {
                return;
            }

            if (Tool.Status != "Available")
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
                        "Your user session could not be found.",
                        "OK");

                    return;
                }

                var activeProject =
                    await GetCurrentWorkerProjectAsync();

                if (activeProject == null)
                {
                    await Shell.Current.DisplayAlert(
                        "No Active Project",
                        "You must be assigned to an active " +
                        "project before borrowing equipment.",
                        "OK");

                    return;
                }

                bool confirm =
                    await Shell.Current.DisplayAlert(
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

                // Worker does NOT evaluate condition.
                // Preserve existing PE/system condition.
                Tool.Condition =
                    existingCondition;

                Tool.BorrowDate =
                    DateTime.Now;

                var success =
                    await _firebase
                        .UpdateToolAsync(Tool);

                if (!success)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not borrow equipment.",
                        "OK");

                    return;
                }

                await LogAsync(
                    "Borrowed",
                    $"Equipment borrowed for " +
                    $"{activeProject.ProjectName}.",
                    existingCondition);

                RefreshToolProperties();

                await Shell.Current.DisplayAlert(
                    "Equipment Borrowed",
                    $"{Tool.ToolName} " +
                    $"({Tool.ToolId}) has been borrowed.\n\n" +
                    $"Project: {activeProject.ProjectName}",
                    "OK");
            }
            catch (Exception ex)
            {
                await LoadToolAsync();

                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not borrow equipment.\n" +
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
        //
        // Worker only SUBMITS return.
        //
        // Worker does NOT:
        // - choose Good / Damaged
        // - choose Minor / Major
        // - create DamageReport
        //
        // PE performs physical inspection.
        // ─────────────────────────────────────────────────────────

        private async Task ReturnAsync()
        {
            if (Tool == null ||
                IsBusy)
            {
                return;
            }

            if (!IsAssignedToMe ||
                Tool.Status != "Borrowed")
            {
                return;
            }

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Return Equipment",
                    $"Return {Tool.ToolName} " +
                    $"({Tool.ToolId})?\n\n" +
                    "Please bring the equipment to the " +
                    "Project Engineer for physical inspection.\n\n" +
                    "You remain responsible for the equipment " +
                    "until the return is approved.",
                    "Submit Return",
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
                        "Your user session could not be found.",
                        "OK");

                    return;
                }

                string currentCondition =
                    string.IsNullOrWhiteSpace(
                        Tool.Condition)
                        ? "Good"
                        : Tool.Condition;

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

                        // Worker does NOT report condition.
                        ReportedCondition =
                            string.Empty,

                        // PE fills this after inspection.
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
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not submit the return request.",
                        "OK");

                    return;
                }

                Tool.Status =
                    "PendingReturn";

                var updated =
                    await _firebase
                        .UpdateToolAsync(Tool);

                if (!updated)
                {
                    // Prevent orphan Pending Return request.
                    request.Status =
                        "Rejected";

                    request.Notes =
                        "Return request cancelled because " +
                        "the equipment status could not be updated.";

                    request.ReviewedDate =
                        DateTime.Now;

                    await _firebase
                        .UpdateReturnRequestAsync(
                            requestKey,
                            request);

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
                    "awaiting Project Engineer physical inspection.",
                    currentCondition);

                RefreshToolProperties();

                await Shell.Current.DisplayAlert(
                    "Return Submitted",
                    $"{Tool.ToolName} ({Tool.ToolId}) " +
                    $"is awaiting Project Engineer inspection.\n\n" +
                    "Please bring the physical equipment " +
                    "to the Project Engineer.",
                    "OK");
            }
            catch (Exception ex)
            {
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
            if (Tool == null ||
                IsBusy)
            {
                return;
            }

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
                        .Where(user =>
                            !string.Equals(
                                user.UniqueKey,
                                CurrentUserKey,
                                StringComparison.OrdinalIgnoreCase) &&
                            user.Role == "Worker" &&
                            user.AccountStatus ==
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
                                .Select(worker =>
                                    worker.FullName)
                                .ToArray());

                if (string.IsNullOrWhiteSpace(
                        selected) ||
                    selected == "Cancel")
                {
                    return;
                }

                var target =
                    workers.FirstOrDefault(worker =>
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
                            Tool.BorrowedProjectId
                            ?? string.Empty,

                        ProjectName =
                            Tool.BorrowedProjectName
                            ?? string.Empty,

                        // Preserve existing condition.
                        // Worker does not re-classify damage.
                        Condition =
                            string.IsNullOrWhiteSpace(
                                Tool.Condition)
                                ? "Good"
                                : Tool.Condition,

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
        // REQUEST BORROW
        // ─────────────────────────────────────────────────────────

        private async Task RequestBorrowAsync()
        {
            if (Tool == null ||
                IsBusy)
            {
                return;
            }

            if (Tool.Status != "Borrowed" ||
                IsAssignedToMe)
            {
                return;
            }

            IsBusy = true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Your user session could not be found.",
                        "OK");

                    return;
                }

                var activeProject =
                    await GetCurrentWorkerProjectAsync();

                if (activeProject == null)
                {
                    await Shell.Current.DisplayAlert(
                        "No Active Project",
                        "You must be assigned to an active " +
                        "project before requesting equipment.",
                        "OK");

                    return;
                }

                bool confirm =
                    await Shell.Current.DisplayAlert(
                        "Request Borrow",
                        $"Send a borrow request for " +
                        $"{Tool.ToolName} ({Tool.ToolId}) " +
                        $"to {Tool.AssignedWorkerName}?\n\n" +
                        $"Your Project: " +
                        $"{activeProject.ProjectName}",
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

                if (result == "DUPLICATE")
                {
                    await Shell.Current.DisplayAlert(
                        "Request Already Pending",
                        $"You already have a pending request for " +
                        $"{Tool.ToolName} ({Tool.ToolId}).",
                        "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(
                        result))
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
                    $"{Tool.AssignedWorkerName}.\n\n" +
                    $"Project: {activeProject.ProjectName}",
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
        //
        // Worker confirms POSSESSION only.
        //
        // Worker does NOT decide Minor / Major.
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
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    "Your user session could not be found.",
                    "OK");

                return;
            }

            if (!ShowConfirmReceipt)
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Assignment",
                    "This equipment is no longer waiting " +
                    "for your confirmation.",
                    "OK");

                await LoadToolAsync();

                return;
            }

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Confirm Receipt",
                    $"Confirm that you physically received " +
                    $"{Tool.ToolName} ({Tool.ToolId})?",
                    "Confirm",
                    "Cancel");

            if (!confirm)
                return;

            IsBusy = true;

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

                // Worker confirms receipt only.
                // Do not classify condition here.
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
                    "Worker physically confirmed receipt " +
                    "of the assigned equipment.",
                    existingCondition);

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
        // END-DAY CHECK-IN
        //
        // Worker reports LOCATION only.
        //
        // Worker does NOT evaluate condition.
        //
        // PE later chooses:
        //
        // Good
        //
        // OR
        //
        // Damaged
        //   └─ Minor Damage
        //   └─ Major Damage
        // ─────────────────────────────────────────────────────────

        private async Task EndDayCheckInAsync()
        {
            if (Tool == null ||
                IsBusy)
            {
                return;
            }

            if (!IsAssignedToMe ||
                Tool.Status != "Borrowed")
            {
                return;
            }

            if (Tool.IsCheckInPending)
            {
                await Shell.Current.DisplayAlert(
                    "Check-In Pending",
                    "This equipment already has an " +
                    "end-day check-in waiting for " +
                    "Project Engineer verification.",
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
                await Shell.Current
                    .DisplayActionSheet(
                        "End Day Check-In",
                        "Cancel",
                        null,
                        locations);

            if (string.IsNullOrWhiteSpace(
                    selectedLocation) ||
                selectedLocation == "Cancel")
            {
                return;
            }

            if (selectedLocation == "Other")
            {
                selectedLocation =
                    await Shell.Current
                        .DisplayPromptAsync(
                            "Storage Location",
                            "Enter where the equipment " +
                            "will be stored:",
                            "Continue",
                            "Cancel",
                            placeholder:
                                "e.g. Building A - Tool Storage");

                if (string.IsNullOrWhiteSpace(
                        selectedLocation))
                {
                    return;
                }

                selectedLocation =
                    selectedLocation.Trim();
            }

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Confirm End-Day Check-In",
                    $"{Tool.ToolName} ({Tool.ToolId})\n\n" +
                    $"Reported Location: {selectedLocation}\n\n" +
                    "The equipment will remain assigned to you.\n\n" +
                    "The Project Engineer will physically " +
                    "verify the equipment and evaluate its condition.",
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
                //
                // Status remains Borrowed.
                // Worker remains responsible.
                // Project remains unchanged.
                //
                // Worker does NOT modify condition.

                var updated =
                    await _firebase
                        .UpdateToolAsync(Tool);

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
                    $"Equipment reported at " +
                    $"{selectedLocation} and is awaiting " +
                    $"Project Engineer physical verification.",
                    Tool.Condition);

                RefreshToolProperties();

                await Shell.Current.DisplayAlert(
                    "Check-In Submitted",
                    $"{Tool.ToolName} ({Tool.ToolId}) " +
                    $"has been submitted for end-day verification.\n\n" +
                    $"Location: {selectedLocation}\n\n" +
                    "The equipment remains assigned to you " +
                    "until further action is taken.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not complete end-day check-in.\n" +
                    $"{ex.Message}",
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
            if (Tool == null ||
                IsBusy)
            {
                return;
            }

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
                    $"You declined " +
                    $"{toolName} ({toolId}).",
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