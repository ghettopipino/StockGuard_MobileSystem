using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    /// <summary>
    /// Worker-facing single-tool detail page.
    /// Exact logic as the original ToolDetailsViewModel with three fixes:
    ///   1. _isLoading = false  (was true — caused infinite spinner on direct open)
    ///   2. ViewHistoryCommand uses //TransactionHistoryView (FlyoutItem, not registered route)
    ///   3. Class renamed to WorkerToolDetailsViewModel so it coexists with
    ///      the admin ToolDetailsViewModel (tool browser)
    /// </summary>
    [QueryProperty(nameof(ToolId), "toolId")]
    public class WorkerToolDetailsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        // ── Query Property ────────────────────────────────────────────────────
        private string _toolId = string.Empty;
        public string ToolId
        {
            get => _toolId;
            set
            {
                SetProperty(ref _toolId, value);
                if (!string.IsNullOrEmpty(value))
                    MainThread.BeginInvokeOnMainThread(
                        async () => await LoadToolAsync());
            }
        }

        // ── Tool Data ─────────────────────────────────────────────────────────
        private Tool? _tool;
        public Tool? Tool
        {
            get => _tool;
            set
            {
                SetProperty(ref _tool, value);
                OnPropertyChanged(nameof(ToolName));
                OnPropertyChanged(nameof(ToolIdDisplay));
                OnPropertyChanged(nameof(ToolIcon));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(AssignedWorkerName));
                OnPropertyChanged(nameof(BorrowDateDisplay));
                OnPropertyChanged(nameof(ConditionText));
                OnPropertyChanged(nameof(ShowBorrow));
                OnPropertyChanged(nameof(ShowTransfer));
                OnPropertyChanged(nameof(ShowReportDamage));
                OnPropertyChanged(nameof(ShowRequestBorrow));
                OnPropertyChanged(nameof(IsAssignedToMe));
                OnPropertyChanged(nameof(ShowPause));
                OnPropertyChanged(nameof(ShowResume));
                OnPropertyChanged(nameof(ShowReturn));
                OnPropertyChanged(nameof(ShowPendingReturn));
                OnPropertyChanged(nameof(ShowConfirmReceipt));
                OnPropertyChanged(nameof(ShowPendingPause));
                OnPropertyChanged(nameof(ShowDeclineReceipt));

            }
        }

        // ── Display Properties ────────────────────────────────────────────────
        public string ToolName => Tool?.ToolName ?? "Loading...";
        public string ToolIdDisplay => Tool?.ToolId ?? string.Empty;
        public string ToolIcon => Tool?.ToolIcon ?? "🔧";
        public string StatusText => Tool?.Status ?? string.Empty;
        public string StatusColor => Tool?.StatusColor ?? "#6b7280";
        public string StatusIcon => Tool?.StatusIcon ?? "❓";
        public string ThemeIcon => _theme.IsDark ? "🌙" : "☀️";
        public bool ShowDeclineReceipt => ShowConfirmReceipt;


        public string AssignedWorkerName =>
            string.IsNullOrEmpty(Tool?.AssignedWorkerName)
                ? "— Not assigned —"
                : Tool.AssignedWorkerName;

        public string BorrowDateDisplay =>
            Tool?.BorrowDate.HasValue == true
                ? Tool.BorrowDate.Value.ToString("MMM d, yyyy h:mm tt")
                : "— Not borrowed —";

        public string ConditionText =>
            string.IsNullOrEmpty(Tool?.Condition) ? "Good" : Tool.Condition;

        // ── Action Visibility ─────────────────────────────────────────────────
        private string CurrentUserKey => _auth.CurrentUser?.UniqueKey ?? string.Empty;

        public bool IsAssignedToMe =>
            Tool != null &&
            !string.IsNullOrEmpty(Tool.AssignedWorkerId) &&
            Tool.AssignedWorkerId == CurrentUserKey;

        public bool ShowBorrow =>
            Tool != null &&
            (Tool.IsAvailable ||
             (Tool.PreAssignedWorkerId == CurrentUserKey && Tool.IsAvailable));

        public bool ShowPause =>
            Tool != null && IsAssignedToMe && Tool.IsBorrowed;

        public bool ShowResume =>
            Tool != null && IsAssignedToMe && Tool.IsOnHold;

        public bool ShowReturn =>
            Tool != null && IsAssignedToMe && (Tool.IsBorrowed || Tool.IsOnHold);

        public bool ShowPendingReturn =>
            Tool != null && IsAssignedToMe && Tool.Status == "PendingReturn";

        public bool ShowTransfer =>
            Tool != null && IsAssignedToMe && Tool.IsBorrowed;

        public bool ShowReportDamage =>
            Tool != null && IsAssignedToMe && (Tool.IsBorrowed || Tool.IsOnHold);

        public bool ShowRequestBorrow =>
            Tool != null && Tool.IsBorrowed && !IsAssignedToMe;

        public bool ShowConfirmReceipt =>
            Tool != null &&
            Tool.PreAssignedWorkerId == CurrentUserKey &&
            Tool.IsAvailable;

        public bool ShowPendingPause =>
            Tool != null && IsAssignedToMe && Tool.IsPendingPause;

        // ── Loading State ─────────────────────────────────────────────────────
        // FIX 1: was `true` — caused infinite spinner when no toolId was set
        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }
        public bool IsNotLoading => !IsLoading;

        private bool _toolNotFound;
        public bool ToolNotFound
        {
            get => _toolNotFound;
            set => SetProperty(ref _toolNotFound, value);
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand BorrowCommand { get; }
        public ICommand ReturnCommand { get; }
        public ICommand TransferCommand { get; }
        public ICommand ReportDamageCommand { get; }
        public ICommand RequestBorrowCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewHistoryCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand ConfirmReceiptCommand { get; }
        public ICommand DeclineCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public WorkerToolDetailsViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            BorrowCommand = new Command(async () => await BorrowAsync(), () => !IsBusy);
            ReturnCommand = new Command(async () => await ReturnAsync(), () => !IsBusy);
            TransferCommand = new Command(async () => await TransferAsync(), () => !IsBusy);
            ReportDamageCommand = new Command(async () => await ReportDamageAsync(), () => !IsBusy);
            RequestBorrowCommand = new Command(async () => await RequestBorrowAsync(), () => !IsBusy);
            PauseCommand = new Command(async () => await PauseAsync(), () => !IsBusy);
            ResumeCommand = new Command(async () => await ResumeAsync(), () => !IsBusy);
            ConfirmReceiptCommand = new Command(async () => await ConfirmReceiptAsync(), () => !IsBusy);
            DeclineCommand = new Command(async () => await DeclineReceiptAsync(), () => !IsBusy);


            // GoBackCommand — ".." pops the detail page off the stack,
            // returning to WorkerDashboardView. Valid here because
            // WorkerToolDetailsView is a registered route (not a FlyoutItem).
            GoBackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
            ToggleThemeCommand = new Command(() => _theme.Toggle());
            RefreshCommand = new Command(async () => await LoadToolAsync());

            // FIX 2: TransactionHistoryView is now a FlyoutItem root page.
            // Must use // absolute route — relative route would throw.
            ViewHistoryCommand = new Command(async () =>
     await Shell.Current.GoToAsync(
         $"//TransactionHistoryView" +
         $"?toolId={Uri.EscapeDataString(ToolId)}&viewMode=worker"));
        }

        // ── Load ──────────────────────────────────────────────────────────────
        private async Task LoadToolAsync()
        {
            if (string.IsNullOrEmpty(ToolId)) return;

            IsLoading = true;
            ToolNotFound = false;

            try
            {
                var tool = await _firebase.GetToolByIdAsync(ToolId);
                if (tool is null) { ToolNotFound = true; return; }
                Tool = tool;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error", $"Could not load tool.\n{ex.Message}", "OK");
            }
            finally { IsLoading = false; }
        }

        // ── Log helper ────────────────────────────────────────────────────────
        private async Task LogAsync(string action, string description, string condition)
        {
            var user = _auth.CurrentUser!;
            await _firebase.LogTransactionAsync(new TransactionLog
            {
                ToolId = Tool!.ToolId,
                ToolName = Tool.ToolName,
                WorkerId = user.UniqueKey,
                WorkerName = user.FullName,
                Action = action,
                Description = description,
                Condition = condition,
                Date = DateTime.Now
            });
        }

        // ── BORROW ────────────────────────────────────────────────────────────
        private async Task BorrowAsync()
        {
            if (Tool is null || IsBusy) return;
            IsBusy = true;
            try
            {
                var condition = await Shell.Current.DisplayActionSheet(
                    "Tool Condition Before Borrowing", "Cancel", null,
                    "Good", "Minor Damage", "Major Damage");
                if (condition == null || condition == "Cancel") return;

                var user = _auth.CurrentUser!;
                Tool.Status = "Borrowed";
                Tool.AssignedWorkerId = user.UniqueKey;
                Tool.AssignedWorkerName = user.FullName;
                Tool.Condition = condition;
                Tool.BorrowDate = DateTime.Now;

                var success = await _firebase.UpdateToolAsync(Tool);
                if (!success)
                {
                    await Shell.Current.DisplayAlert("Error", "Could not borrow tool. Try again.", "OK");
                    return;
                }

                await LogAsync("Borrowed", $"Borrowed in {condition} condition", condition);
                await LoadToolAsync();
                await Shell.Current.DisplayAlert("✅ Tool Borrowed",
                    $"You borrowed {Tool.ToolName} ({Tool.ToolId}) successfully.", "OK");
            }
            finally { IsBusy = false; }
        }

        // ── RETURN ────────────────────────────────────────────────────────────
        // ── RETURN ────────────────────────────────────────────────────────────────
        private async Task ReturnAsync()
        {
            if (Tool is null || IsBusy)
                return;

            if (!IsAssignedToMe)
                return;

            if (!(Tool.IsBorrowed || Tool.IsOnHold))
                return;

            IsBusy = true;

            try
            {
                var condition = await Shell.Current.DisplayActionSheet(
                    "Tool Condition On Return",
                    "Cancel",
                    null,
                    "Good",
                    "Minor Damage",
                    "Major Damage");

                if (condition == null || condition == "Cancel")
                    return;

                bool confirm = await Shell.Current.DisplayAlert(
                    "Return Tool",
                    $"Submit {Tool.ToolName} ({Tool.ToolId}) for return?\n\n" +
                    "Please bring the tool to the Project Engineer for physical inspection. " +
                    "The tool will remain assigned to you until the return is verified.",
                    "Submit Return",
                    "Cancel");

                if (!confirm)
                    return;

                var user = _auth.CurrentUser;

                if (user == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Your user session could not be found.",
                        "OK");

                    return;
                }

                var request = new ReturnRequest
                {
                    ToolId = Tool.ToolId,
                    ToolName = Tool.ToolName,

                    WorkerId = user.UniqueKey,
                    WorkerName = user.FullName,

                    ProjectId = Tool.BorrowedProjectId ?? string.Empty,
                    ProjectName = Tool.BorrowedProjectName ?? string.Empty,

                    ReportedCondition = condition,
                    VerifiedCondition = string.Empty,

                    Status = "Pending",
                    RequestDate = DateTime.Now
                };

                var requestKey =
                    await _firebase.CreateReturnRequestAsync(request);

                if (string.IsNullOrEmpty(requestKey))
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not submit the return request. Please check your connection and try again.",
                        "OK");

                    return;
                }

                // IMPORTANT:
                // Worker remains accountable until PE verifies.
                Tool.Status = "PendingReturn";

                // Do NOT clear:
                // AssignedWorkerId
                // AssignedWorkerName
                // BorrowDate
                // BorrowedProjectId
                // BorrowedProjectName

                var updated =
                    await _firebase.UpdateToolAsync(Tool);

                if (!updated)
                {
                    request.Status = "Rejected";
                    request.Notes =
                        "Return request cancelled because tool status could not be updated.";

                    await _firebase.UpdateReturnRequestAsync(
                        requestKey,
                        request);

                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The return request could not be completed because the tool status failed to update.",
                        "OK");

                    return;
                }

                await LogAsync(
                    "Return Requested",
                    $"Return requested. Worker reported condition: {condition}. Awaiting Project Engineer verification.",
                    condition);

                await LoadToolAsync();

                await Shell.Current.DisplayAlert(
                    "Return Request Submitted",
                    $"{Tool.ToolName} ({Tool.ToolId}) is now pending return verification.\n\n" +
                    "Please bring the tool to the Project Engineer. " +
                    "You remain responsible for the tool until the Project Engineer confirms the return.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not submit return request.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        // ── TRANSFER ──────────────────────────────────────────────────────────
        private async Task TransferAsync()
        {
            if (Tool is null || IsBusy) return;
            IsBusy = true;
            try
            {
                var allUsers = await _firebase.GetAllUsersAsync();
                var workers = allUsers
                    .Where(u => u.UniqueKey != CurrentUserKey &&
                                u.Role == "Worker" &&
                                u.AccountStatus == "Approved")
                    .ToList();

                if (workers.Count == 0)
                {
                    await Shell.Current.DisplayAlert("No Workers",
                        "No other workers available to transfer to.", "OK");
                    return;
                }

                var selected = await Shell.Current.DisplayActionSheet(
                    "Transfer To", "Cancel", null,
                    workers.Select(w => w.FullName).ToArray());
                if (selected == null || selected == "Cancel") return;

                var target = workers.FirstOrDefault(w => w.FullName == selected);
                if (target is null) return;

                var user = _auth.CurrentUser!;
                var request = new TransferRequest
                {
                    ToolId = Tool.ToolId,
                    ToolName = Tool.ToolName,
                    FromWorkerId = user.UniqueKey,
                    FromWorkerName = user.FullName,
                    ToWorkerId = target.UniqueKey,
                    ToWorkerName = target.FullName,
                    Status = "Pending",
                    RequestDate = DateTime.Now
                };

                var key = await _firebase.CreateTransferRequestAsync(request);
                if (string.IsNullOrEmpty(key))
                {
                    await Shell.Current.DisplayAlert("Error",
                        "Could not send transfer request. Please check your connection.", "OK");
                    return;
                }

                await Shell.Current.DisplayAlert("✅ Transfer Request Sent",
                    $"Transfer request sent to {target.FullName}.\n\nThe tool will be transferred once they accept the request.", "OK");
            }
            finally { IsBusy = false; }
        }

        // ── REPORT DAMAGE ─────────────────────────────────────────────────────
        private async Task ReportDamageAsync()
        {
            if (Tool is null || IsBusy) return;
            IsBusy = true;
            try
            {
                var severity = await Shell.Current.DisplayActionSheet(
                    "Damage Severity", "Cancel", null, "Minor Damage", "Major Damage");
                if (severity == null || severity == "Cancel") return;

                var description = await Shell.Current.DisplayPromptAsync(
                    "Damage Description", "Briefly describe the damage:",
                    "Submit", "Cancel", placeholder: "e.g. Handle broken, not functional");
                if (string.IsNullOrWhiteSpace(description)) return;

                var user = _auth.CurrentUser!;
                var report = new DamageReport
                {
                    ToolId = Tool.ToolId,
                    ToolName = Tool.ToolName,
                    WorkerId = user.UniqueKey,
                    WorkerName = user.FullName,
                    Description = description,
                    Severity = severity,
                    Status = "Pending",
                    ReportDate = DateTime.Now
                };

                await _firebase.SubmitDamageReportAsync(report);
                Tool.Status = "Damaged";
                Tool.Condition = severity;
                await _firebase.UpdateToolAsync(Tool);
                await LogAsync("Damaged", $"Damage reported: {severity} — {description}", severity);
                await LoadToolAsync();
                await Shell.Current.DisplayAlert("✅ Damage Reported",
                    $"Damage report submitted for {Tool.ToolName} ({Tool.ToolId}).\n\nThe Project Engineer has been notified.", "OK");
            }
            finally { IsBusy = false; }
        }

        // ── REQUEST BORROW ────────────────────────────────────────────────────
        private async Task RequestBorrowAsync()
        {
            if (Tool is null || IsBusy) return;
            IsBusy = true;
            try
            {
                var user = _auth.CurrentUser!;
                bool confirm = await Shell.Current.DisplayAlert("Request Borrow",
                    $"Send a borrow request for {Tool.ToolName} ({Tool.ToolId}) to {Tool.AssignedWorkerName}?",
                    "Send Request", "Cancel");
                if (!confirm) return;

                var request = new BorrowRequest
                {
                    ToolId = Tool.ToolId,
                    ToolName = Tool.ToolName,
                    RequesterId = user.UniqueKey,
                    RequesterName = user.FullName,
                    OwnerId = Tool.AssignedWorkerId,
                    OwnerName = Tool.AssignedWorkerName,
                    Status = "Pending",
                    RequestDate = DateTime.Now
                };

                var key = await _firebase.CreateBorrowRequestAsync(request);
                if (string.IsNullOrEmpty(key))
                {
                    await Shell.Current.DisplayAlert("Error",
                        "Request could not be saved. Please check your connection.", "OK");
                    return;
                }

                await Shell.Current.DisplayAlert("✅ Request Sent",
                    $"Your borrow request has been sent to {Tool.AssignedWorkerName}.\n\nYou will be notified when they respond.", "OK");
            }
            finally { IsBusy = false; }
        }

        // ── PAUSE ─────────────────────────────────────────────────────────────
        private async Task PauseAsync()
        {
            if (Tool is null || IsBusy) return;
            IsBusy = true;
            try
            {
                var reason = await Shell.Current.DisplayPromptAsync(
                    "Pause Borrowing", "Reason for pausing:\n(e.g. End of day, Site storage)",
                    "Submit", "Cancel", placeholder: "e.g. Storing at site overnight");
                if (string.IsNullOrWhiteSpace(reason)) return;

                bool confirm = await Shell.Current.DisplayAlert("⏸️ Pause Borrowing",
                    $"Physically bring {Tool.ToolName} ({Tool.ToolId}) to the project site storage.\n\nThe Project Engineer will verify and approve your pause request.",
                    "Submit Request", "Cancel");
                if (!confirm) return;

                var user = _auth.CurrentUser!;
                var projectId = Tool.BorrowedProjectId;
                var projectName = Tool.BorrowedProjectName;

                var request = new PauseRequest
                {
                    ToolId = Tool.ToolId,
                    ToolName = Tool.ToolName,
                    WorkerId = user.UniqueKey,
                    WorkerName = user.FullName,
                    ProjectId = projectId,
                    ProjectName = projectName,
                    Reason = reason.Trim(),
                    Status = "Pending",
                    RequestDate = DateTime.Now
                };

                var key = await _firebase.CreatePauseRequestAsync(request);
                if (string.IsNullOrEmpty(key))
                {
                    await Shell.Current.DisplayAlert("Error", "Could not submit pause request.", "OK");
                    return;
                }

                Tool.Status = "PendingPause";
                await _firebase.UpdateToolAsync(Tool);
                await LogAsync("Paused", $"Pause requested: {reason}", Tool.Condition);
                await LoadToolAsync();
                await Shell.Current.DisplayAlert("⏸️ Pause Request Submitted",
                    $"Your pause request has been sent to the Project Engineer.\n\nPlease bring {Tool.ToolName} to the project site storage.\n\nThe PE will verify and approve once physically checked.", "OK");
            }
            finally { IsBusy = false; }
        }

        // ── RESUME ────────────────────────────────────────────────────────────
        // ── RESUME ────────────────────────────────────────────────────────────────
        private async Task ResumeAsync()
        {
            if (Tool is null || IsBusy)
                return;

            if (!IsAssignedToMe)
                return;

            if (!Tool.IsOnHold)
                return;

            IsBusy = true;

            try
            {
                bool confirm =
                    await Shell.Current.DisplayAlert(
                        "Resume Borrowing",
                        $"Resume using {Tool.ToolName} ({Tool.ToolId})?\n\n" +
                        "You are taking the tool back from project site storage.",
                        "Resume",
                        "Cancel");

                if (!confirm)
                    return;

                // Return to active borrowed state.
                Tool.Status = "Borrowed";

                // IMPORTANT:
                // Do NOT change:
                //
                // AssignedWorkerId
                // AssignedWorkerName
                // BorrowedProjectId
                // BorrowedProjectName
                // BorrowDate
                //
                // This is still the same borrowing session.

                // Clear temporary hold information.
                Tool.HoldProjectId =
                    string.Empty;

                Tool.HoldProjectName =
                    string.Empty;

                Tool.HoldLocation =
                    string.Empty;

                Tool.HoldDate =
                    null;

                Tool.LastBorrowerId =
                    string.Empty;

                Tool.LastBorrowerName =
                    string.Empty;

                var success =
                    await _firebase
                        .UpdateToolAsync(Tool);

                if (!success)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not resume borrowing. Please try again.",
                        "OK");

                    return;
                }

                await LogAsync(
                    "Resumed",
                    "Resumed borrowing from project site storage",
                    Tool.Condition);

                await LoadToolAsync();

                await Shell.Current.DisplayAlert(
                    "Borrowing Resumed",
                    $"You resumed using " +
                    $"{Tool.ToolName} ({Tool.ToolId}).",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not resume borrowing.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── CONFIRM RECEIPT ───────────────────────────────────────────────────
        private async Task ConfirmReceiptAsync()
        {
            if (Tool is null || IsBusy) return;
            IsBusy = true;
            try
            {
                var condition = await Shell.Current.DisplayActionSheet(
                    "Tool Condition", "Cancel", null,
                    "Good", "Minor Damage", "Major Damage");
                if (condition == null || condition == "Cancel") return;

                var user = _auth.CurrentUser!;
                Tool.Status = "Borrowed";
                Tool.AssignedWorkerId = user.UniqueKey;
                Tool.AssignedWorkerName = user.FullName;
                Tool.Condition = condition;
                Tool.BorrowDate = DateTime.Now;
                Tool.PreAssignedWorkerId = string.Empty;
                Tool.PreAssignedWorkerName = string.Empty;

                await _firebase.UpdateToolAsync(Tool);
                await LogAsync("Borrowed",
                    $"Confirmed receipt of pre-assigned tool in {condition} condition", condition);
                await LoadToolAsync();
                await Shell.Current.DisplayAlert("✅ Receipt Confirmed",
                    $"You have confirmed receipt of {Tool.ToolName} ({Tool.ToolId}).\n\nThe tool is now assigned to you.", "OK");
            }
            finally { IsBusy = false; }
        }

        private async Task DeclineReceiptAsync()
        {
            if (Tool is null || IsBusy) return;
            IsBusy = true;
            try
            {
                bool confirm = await Shell.Current.DisplayAlert("Decline Assignment",
                    $"Decline {Tool.ToolName} ({Tool.ToolId})?\n\nIt will go back to Available for the Project Engineer to reassign.",
                    "Decline", "Cancel");
                if (!confirm) return;

                var toolName = Tool.ToolName;
                var toolId = Tool.ToolId;

                Tool.PreAssignedWorkerId = string.Empty;
                Tool.PreAssignedWorkerName = string.Empty;

                await _firebase.UpdateToolAsync(Tool);
                await LogAsync("Declined", "Declined pre-assigned tool", Tool.Condition);
                await LoadToolAsync();
                await Shell.Current.DisplayAlert("Declined",
                    $"You declined {toolName} ({toolId}).", "OK");
            }
            //}
            finally { IsBusy = false; }
        }
    }
}