using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    public class PauseRequestsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        public ObservableCollection<ReturnRequestResult>
            PendingReturnRequests
        { get; } = new();

        public ObservableCollection<ReturnRequestResult>
            ProcessedReturnRequests
        { get; } = new();

        public ObservableCollection<Tool>
            PendingCheckIns
        { get; } = new();

        private int _pendingCount;
        public int PendingCount
        {
            get => _pendingCount;
            private set => SetProperty(ref _pendingCount, value);
        }

        private int _approvedCount;
        public int ApprovedCount
        {
            get => _approvedCount;
            private set => SetProperty(ref _approvedCount, value);
        }

        public bool NoPendingReturn =>
            PendingReturnRequests.Count == 0;

        public bool NoPendingCheckIns =>
            PendingCheckIns.Count == 0;

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public ICommand OpenFlyoutCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public ICommand ApproveReturnCommand { get; }
        public ICommand RejectReturnCommand { get; }
        public ICommand VerifyCheckInCommand { get; }

        public PauseRequestsViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;

            Title = "Return & Check-In";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(
                    () => OnPropertyChanged(nameof(ThemeIcon)));

            OpenFlyoutCommand =
                new Command(() =>
                {
                    if (Shell.Current != null)
                        Shell.Current.FlyoutIsPresented = true;
                });

            RefreshCommand =
                new Command(
                    async () => await RefreshAsync());

            ToggleThemeCommand =
                new Command(() => _theme.Toggle());

            ApproveReturnCommand =
                new Command<ReturnRequestResult>(
                    async item =>
                        await ApproveReturnAsync(item));

            RejectReturnCommand =
                new Command<ReturnRequestResult>(
                    async item =>
                        await RejectReturnAsync(item));

            VerifyCheckInCommand =
                new Command<Tool>(
                    async tool =>
                        await VerifyCheckInAsync(tool));

            MainThread.BeginInvokeOnMainThread(
                async () => await LoadAsync());
        }

        // ─────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var user = _auth.CurrentUser;

                if (user == null)
                {
                    PendingReturnRequests.Clear();
                    ProcessedReturnRequests.Clear();
                    PendingCheckIns.Clear();

                    PendingCount = 0;
                    ApprovedCount = 0;

                    OnPropertyChanged(nameof(NoPendingReturn));
                    OnPropertyChanged(nameof(NoPendingCheckIns));

                    return;
                }

                var returnRequestsTask =
                    _firebase.GetAllReturnRequestsRawAsync();

                var allToolsTask =
                    _firebase.GetAllToolsAsync(
                        forceRefresh: true);

                var projectsTask =
                    _firebase.GetAllProjectsAsync();

                await Task.WhenAll(
                    returnRequestsTask,
                    allToolsTask,
                    projectsTask);

                var returnRequests =
                    returnRequestsTask.Result ??
                    new List<ReturnRequestResult>();

                var allTools =
                    allToolsTask.Result ??
                    new List<Tool>();

                var projects =
                    projectsTask.Result ??
                    new List<Project>();

                var myProjectIds =
                    projects
                        .Where(p =>
                            !p.IsDeleted &&
                            p.CreatedBy == user.UniqueKey)
                        .Select(p => p.ProjectId)
                        .ToHashSet();

                PendingReturnRequests.Clear();
                ProcessedReturnRequests.Clear();
                PendingCheckIns.Clear();

                // ── CHECK-INS ─────────────────────────────

                var pendingCheckIns =
                    allTools
                        .Where(t =>
                            t.Status == "Borrowed" &&
                            t.IsCheckInPending &&
                            myProjectIds.Contains(
                                t.BorrowedProjectId))
                        .OrderByDescending(t =>
                            t.LastCheckInDate)
                        .ToList();

                foreach (var tool in pendingCheckIns)
                    PendingCheckIns.Add(tool);

                // ── PENDING RETURNS ───────────────────────

                var pendingReturn =
                    returnRequests
                        .Where(r =>
                        {
                            if (r.Request.Status != "Pending")
                                return false;

                            if (!myProjectIds.Contains(
                                    r.Request.ProjectId))
                                return false;

                            var tool =
                                allTools.FirstOrDefault(t =>
                                    t.ToolId ==
                                    r.Request.ToolId);

                            return tool != null &&
                                   tool.Status ==
                                   "PendingReturn";
                        })
                        .OrderByDescending(r =>
                            r.Request.RequestDate)
                        .ToList();

                foreach (var item in pendingReturn)
                    PendingReturnRequests.Add(item);

                // ── PROCESSED RETURNS ─────────────────────

                var processedReturn =
                    returnRequests
                        .Where(r =>
                            r.Request.Status != "Pending" &&
                            myProjectIds.Contains(
                                r.Request.ProjectId))
                        .OrderByDescending(r =>
                            r.Request.RequestDate)
                        .Take(10)
                        .ToList();

                foreach (var item in processedReturn)
                    ProcessedReturnRequests.Add(item);

                UpdateStats();

                OnPropertyChanged(nameof(NoPendingReturn));
                OnPropertyChanged(nameof(NoPendingCheckIns));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Load Return/Check-In Requests error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateStats()
        {
            PendingCount =
                PendingReturnRequests.Count;

            ApprovedCount =
                ProcessedReturnRequests.Count(r =>
                    r.Request.Status == "Approved");
        }

        private async Task RefreshAsync()
        {
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

        // ─────────────────────────────────────────────────────
        // VERIFY END-DAY CHECK-IN
        // ─────────────────────────────────────────────────────

        private async Task VerifyCheckInAsync(
            Tool tool)
        {
            if (tool == null || IsBusy)
                return;

            if (!tool.IsCheckInPending ||
                tool.Status != "Borrowed")
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Check-In",
                    "This equipment no longer has a pending end-day check-in.",
                    "OK");

                await LoadAsync();
                return;
            }

            var condition =
                await Shell.Current.DisplayActionSheet(
                    "Equipment Condition",
                    "Cancel",
                    null,
                    "Good",
                    "Damaged");

            if (string.IsNullOrWhiteSpace(condition) ||
                condition == "Cancel")
            {
                return;
            }

            string severity = string.Empty;
            string damageDescription = string.Empty;

            if (condition == "Damaged")
            {
                var selectedSeverity =
                    await Shell.Current.DisplayActionSheet(
                        "Damage Severity",
                        "Cancel",
                        null,
                        "Minor Damage",
                        "Major Damage");

                if (string.IsNullOrWhiteSpace(
                        selectedSeverity) ||
                    selectedSeverity == "Cancel")
                {
                    return;
                }

                severity =
                    selectedSeverity;

                var description =
                    await Shell.Current.DisplayPromptAsync(
                        "Damage Description",
                        "Describe the damage found during end-day inspection:",
                        "Continue",
                        "Cancel",
                        placeholder:
                            "e.g. Handle cracked during use");

                if (string.IsNullOrWhiteSpace(
                        description))
                {
                    return;
                }

                damageDescription =
                    description.Trim();
            }

            string conditionText =
                condition == "Good"
                    ? "Condition: Good"
                    : $"Condition: Damaged\nSeverity: {severity}";

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Verify End-Day Check-In",
                    $"Confirm that you physically inspected " +
                    $"{tool.ToolName} ({tool.ToolId}).\n\n" +
                    $"Worker: {tool.AssignedWorkerName}\n" +
                    $"Project: {tool.BorrowedProjectName}\n" +
                    $"Location: {tool.LastCheckInLocation}\n" +
                    $"{conditionText}",
                    "Verify",
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

                string workerId =
                    tool.AssignedWorkerId;

                string workerName =
                    tool.AssignedWorkerName;

                string projectId =
                    tool.BorrowedProjectId;

                string projectName =
                    tool.BorrowedProjectName;

                string location =
                    tool.LastCheckInLocation;

                // ── GOOD CHECK-IN ─────────────────────────

                if (condition == "Good")
                {
                    tool.IsCheckInPending =
                        false;

                    tool.LastCheckInVerifiedById =
                        user.UniqueKey;

                    tool.LastCheckInVerifiedByName =
                        user.FullName;

                    tool.Condition =
                        "Good";

                    var updated =
                        await _firebase.UpdateToolAsync(tool);

                    if (!updated)
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "Could not verify the end-day check-in.",
                            "OK");

                        return;
                    }

                    await _firebase.LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId =
                                tool.ToolId,

                            ToolName =
                                tool.ToolName,

                            WorkerId =
                                workerId,

                            WorkerName =
                                workerName,

                            ProjectId =
                                projectId,

                            ProjectName =
                                projectName,

                            PerformedById =
                                user.UniqueKey,

                            PerformedByName =
                                user.FullName,

                            Action =
                                "End Day Check-In Verified",

                            Description =
                                $"Equipment physically verified " +
                                $"in good condition at {location}.",

                            Condition =
                                "Good",

                            Date =
                                DateTime.Now
                        });

                    await Shell.Current.DisplayAlert(
                        "Check-In Verified",
                        $"{tool.ToolName} ({tool.ToolId}) was verified.\n\n" +
                        $"Condition: Good\n" +
                        $"Location: {location}\n\n" +
                        $"The equipment remains assigned to {workerName}.",
                        "OK");
                }

                // ── DAMAGED CHECK-IN ──────────────────────

                else
                {
                    var damageReport =
                        new DamageReport
                        {
                            ToolId =
                                tool.ToolId,

                            ToolName =
                                tool.ToolName,

                            WorkerId =
                                workerId,

                            WorkerName =
                                workerName,

                            ProjectId =
                                projectId,

                            ProjectName =
                                projectName,

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

                    var reportKey =
                        await _firebase.SubmitDamageReportAsync(
                            damageReport);

                    if (string.IsNullOrWhiteSpace(
                            reportKey))
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "Could not create the damage report.",
                            "OK");

                        return;
                    }

                    tool.IsCheckInPending =
                        false;

                    tool.LastCheckInVerifiedById =
                        user.UniqueKey;

                    tool.LastCheckInVerifiedByName =
                        user.FullName;

                    tool.Status =
                        "Damaged";

                    tool.Condition =
                        severity;

                    var updated =
                        await _firebase.UpdateToolAsync(tool);

                    if (!updated)
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "Damage report was created, but the equipment status could not be updated.",
                            "OK");

                        return;
                    }

                    await _firebase.LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId =
                                tool.ToolId,

                            ToolName =
                                tool.ToolName,

                            WorkerId =
                                workerId,

                            WorkerName =
                                workerName,

                            ProjectId =
                                projectId,

                            ProjectName =
                                projectName,

                            PerformedById =
                                user.UniqueKey,

                            PerformedByName =
                                user.FullName,

                            Action =
                                "Damage Reported",

                            Description =
                                $"Damage discovered during end-day " +
                                $"inspection at {location}. " +
                                $"{severity} — {damageDescription}",

                            Condition =
                                severity,

                            Date =
                                DateTime.Now
                        });

                    await Shell.Current.DisplayAlert(
                        "Damage Found",
                        $"{tool.ToolName} ({tool.ToolId}) was found damaged.\n\n" +
                        $"Severity: {severity}\n" +
                        $"Location: {location}\n\n" +
                        "A damage report has been created.",
                        "OK");
                }

                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not verify check-in.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────
        // APPROVE RETURN
        // ─────────────────────────────────────────────────────

        private async Task ApproveReturnAsync(
            ReturnRequestResult item)
        {
            if (item is null || IsBusy)
                return;

            var request =
                item.Request;

            var condition =
                await Shell.Current.DisplayActionSheet(
                    "Equipment Condition",
                    "Cancel",
                    null,
                    "Good",
                    "Damaged");

            if (string.IsNullOrWhiteSpace(condition) ||
                condition == "Cancel")
            {
                return;
            }

            string severity =
                string.Empty;

            string damageDescription =
                string.Empty;

            if (condition == "Damaged")
            {
                var selectedSeverity =
                    await Shell.Current.DisplayActionSheet(
                        "Damage Severity",
                        "Cancel",
                        null,
                        "Minor Damage",
                        "Major Damage");

                if (string.IsNullOrWhiteSpace(
                        selectedSeverity) ||
                    selectedSeverity == "Cancel")
                {
                    return;
                }

                severity =
                    selectedSeverity;

                var description =
                    await Shell.Current.DisplayPromptAsync(
                        "Damage Description",
                        "Describe the damage found during inspection:",
                        "Continue",
                        "Cancel",
                        placeholder:
                            "e.g. Power cable damaged");

                if (string.IsNullOrWhiteSpace(description))
                    return;

                damageDescription =
                    description.Trim();
            }

            string conditionDetails =
                condition == "Damaged"
                    ? $"Condition: Damaged\nSeverity: {severity}"
                    : "Condition: Good";

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Approve Return",
                    $"Confirm that you physically received " +
                    $"{request.ToolName} ({request.ToolId}).\n\n" +
                    $"Worker: {request.WorkerName}\n" +
                    $"Project: {request.ProjectName}\n" +
                    $"{conditionDetails}",
                    "Approve Return",
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
                    await _firebase.GetToolByIdAsync(
                        request.ToolId);

                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The equipment could not be found.",
                        "OK");

                    return;
                }

                if (tool.Status != "PendingReturn")
                {
                    await Shell.Current.DisplayAlert(
                        "Invalid Return",
                        "This equipment is no longer pending return.",
                        "OK");

                    await LoadAsync();
                    return;
                }

                string workerId =
                    request.WorkerId;

                string workerName =
                    request.WorkerName;

                string projectId =
                    request.ProjectId;

                string projectName =
                    request.ProjectName;

                request.Status =
                    "Approved";

                request.VerifiedCondition =
                    condition;

                request.ReviewedDate =
                    DateTime.Now;

                request.ReviewedById =
                    user.UniqueKey;

                request.ReviewedByName =
                    user.FullName;

                var requestUpdated =
                    await _firebase.UpdateReturnRequestAsync(
                        item.Key,
                        request);

                if (!requestUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not update the return request.",
                        "OK");

                    return;
                }

                // ── GOOD RETURN ───────────────────────────

                if (condition == "Good")
                {
                    tool.Status =
                        "Available";

                    tool.Condition =
                        "Good";

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

                    ClearCheckInData(tool);

                    var toolUpdated =
                        await _firebase.UpdateToolAsync(tool);

                    if (!toolUpdated)
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "Return was approved, but the equipment could not be updated.",
                            "OK");

                        return;
                    }

                    await _firebase.LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId =
                                tool.ToolId,

                            ToolName =
                                tool.ToolName,

                            WorkerId =
                                workerId,

                            WorkerName =
                                workerName,

                            ProjectId =
                                projectId,

                            ProjectName =
                                projectName,

                            PerformedById =
                                user.UniqueKey,

                            PerformedByName =
                                user.FullName,

                            Action =
                                "Returned",

                            Description =
                                $"Return inspected and approved " +
                                $"by {user.FullName}. " +
                                "Equipment returned in good condition.",

                            Condition =
                                "Good",

                            Date =
                                DateTime.Now
                        });

                    await Shell.Current.DisplayAlert(
                        "Return Approved",
                        $"{tool.ToolName} ({tool.ToolId}) has been returned.\n\n" +
                        "Condition: Good\n" +
                        "The equipment is now Available.",
                        "OK");
                }

                // ── DAMAGED RETURN ────────────────────────

                else
                {
                    var damageReport =
                        new DamageReport
                        {
                            ToolId =
                                tool.ToolId,

                            ToolName =
                                tool.ToolName,

                            WorkerId =
                                workerId,

                            WorkerName =
                                workerName,

                            ProjectId =
                                projectId,

                            ProjectName =
                                projectName,

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

                    var damageReportKey =
                        await _firebase.SubmitDamageReportAsync(
                            damageReport);

                    if (string.IsNullOrEmpty(
                            damageReportKey))
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "The return was approved, but the damage report could not be created.",
                            "OK");

                        return;
                    }

                    tool.Status =
                        "Damaged";

                    tool.Condition =
                        severity;

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

                    ClearCheckInData(tool);

                    var toolUpdated =
                        await _firebase.UpdateToolAsync(tool);

                    if (!toolUpdated)
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "The damage report was created, but the equipment status could not be updated.",
                            "OK");

                        return;
                    }

                    await _firebase.LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId =
                                tool.ToolId,

                            ToolName =
                                tool.ToolName,

                            WorkerId =
                                workerId,

                            WorkerName =
                                workerName,

                            ProjectId =
                                projectId,

                            ProjectName =
                                projectName,

                            PerformedById =
                                user.UniqueKey,

                            PerformedByName =
                                user.FullName,

                            Action =
                                "Returned Damaged",

                            Description =
                                $"Return inspected by {user.FullName}. " +
                                $"Damage found: {severity} — " +
                                $"{damageDescription}",

                            Condition =
                                severity,

                            Date =
                                DateTime.Now
                        });

                    await Shell.Current.DisplayAlert(
                        "Damaged Return Accepted",
                        $"{tool.ToolName} ({tool.ToolId}) has been returned.\n\n" +
                        $"Damage: {severity}\n" +
                        "A damage report was created automatically.",
                        "OK");
                }

                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not approve return.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────
        // REJECT RETURN
        // ─────────────────────────────────────────────────────

        private async Task RejectReturnAsync(
            ReturnRequestResult item)
        {
            if (item is null || IsBusy)
                return;

            var request =
                item.Request;

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Reject Return",
                    $"Reject the return request for " +
                    $"{request.ToolName} ({request.ToolId})?\n\n" +
                    "Use Reject only when the physical return was " +
                    "not accepted or could not be completed.\n\n" +
                    $"The equipment will remain assigned to " +
                    $"{request.WorkerName}.",
                    "Reject",
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
                    await _firebase.GetToolByIdAsync(
                        request.ToolId);

                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The equipment could not be found.",
                        "OK");

                    return;
                }

                if (tool.Status != "PendingReturn")
                {
                    await Shell.Current.DisplayAlert(
                        "Invalid Return",
                        "This equipment is no longer pending return.",
                        "OK");

                    await LoadAsync();
                    return;
                }

                request.Status =
                    "Rejected";

                request.ReviewedDate =
                    DateTime.Now;

                request.ReviewedById =
                    user.UniqueKey;

                request.ReviewedByName =
                    user.FullName;

                var requestUpdated =
                    await _firebase.UpdateReturnRequestAsync(
                        item.Key,
                        request);

                if (!requestUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not reject the return request.",
                        "OK");

                    return;
                }

                tool.Status =
                    "Borrowed";

                tool.AssignedWorkerId =
                    request.WorkerId;

                tool.AssignedWorkerName =
                    request.WorkerName;

                tool.BorrowedProjectId =
                    request.ProjectId;

                tool.BorrowedProjectName =
                    request.ProjectName;

                var toolUpdated =
                    await _firebase.UpdateToolAsync(tool);

                if (!toolUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The request was rejected, but the equipment status could not be restored.",
                        "OK");

                    return;
                }

                await _firebase.LogTransactionAsync(
                    new TransactionLog
                    {
                        ToolId =
                            tool.ToolId,

                        ToolName =
                            tool.ToolName,

                        WorkerId =
                            request.WorkerId,

                        WorkerName =
                            request.WorkerName,

                        ProjectId =
                            request.ProjectId,

                        ProjectName =
                            request.ProjectName,

                        PerformedById =
                            user.UniqueKey,

                        PerformedByName =
                            user.FullName,

                        Action =
                            "Return Rejected",

                        Description =
                            $"Return rejected by {user.FullName}. " +
                            $"Equipment remains assigned to " +
                            $"{request.WorkerName}.",

                        Condition =
                            tool.Condition,

                        Date =
                            DateTime.Now
                    });

                await Shell.Current.DisplayAlert(
                    "Return Rejected",
                    $"{request.ToolName} remains assigned to " +
                    $"{request.WorkerName}.",
                    "OK");

                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not reject return.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static void ClearCheckInData(
            Tool tool)
        {
            tool.LastCheckInLocation =
                string.Empty;

            tool.LastCheckInDate =
                null;

            tool.IsCheckInPending =
                false;

            tool.LastCheckInVerifiedById =
                string.Empty;

            tool.LastCheckInVerifiedByName =
                string.Empty;
        }
    }
}