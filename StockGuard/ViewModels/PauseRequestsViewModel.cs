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

        // Separate from IsBusy so LoadAsync can run
        // after Approve / Reject operations.
        private bool _isLoading;

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
            private set =>
                SetProperty(
                    ref _pendingCount,
                    value);
        }


        private int _approvedCount;

        public int ApprovedCount
        {
            get => _approvedCount;
            private set =>
                SetProperty(
                    ref _approvedCount,
                    value);
        }


        public bool NoPendingReturn =>
            PendingReturnRequests.Count == 0;

        public bool NoPendingCheckIns =>
            PendingCheckIns.Count == 0;


        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set =>
                SetProperty(
                    ref _isRefreshing,
                    value);
        }


        // ═══════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════

        public ICommand OpenFlyoutCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public ICommand ApproveReturnCommand { get; }
        public ICommand RejectReturnCommand { get; }

        public ICommand VerifyCheckInCommand { get; }
        public ICommand RejectCheckInCommand { get; }


        // ═══════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════

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
                    () =>
                        OnPropertyChanged(
                            nameof(ThemeIcon)));

            OpenFlyoutCommand =
                new Command(() =>
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
                    () => _theme.Toggle());


            ApproveReturnCommand =
                new Command<ReturnRequestResult>(
                    async item =>
                        await ApproveReturnAsync(
                            item));

            RejectReturnCommand =
                new Command<ReturnRequestResult>(
                    async item =>
                        await RejectReturnAsync(
                            item));


            VerifyCheckInCommand =
                new Command<Tool>(
                    async tool =>
                        await VerifyCheckInAsync(
                            tool));

            RejectCheckInCommand =
                new Command<Tool>(
                    async tool =>
                        await RejectCheckInAsync(
                            tool));


            MainThread.BeginInvokeOnMainThread(
                async () =>
                    await LoadAsync());
        }


        // ═══════════════════════════════════════════════
        // LOAD
        // ═══════════════════════════════════════════════

        public async Task LoadAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;
            IsBusy = true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                {
                    PendingReturnRequests.Clear();
                    ProcessedReturnRequests.Clear();
                    PendingCheckIns.Clear();

                    PendingCount = 0;
                    ApprovedCount = 0;

                    OnPropertyChanged(
                        nameof(NoPendingReturn));

                    OnPropertyChanged(
                        nameof(NoPendingCheckIns));

                    return;
                }


                var returnRequestsTask =
                    _firebase
                        .GetAllReturnRequestsRawAsync();

                var allToolsTask =
                    _firebase
                        .GetAllToolsAsync(
                            forceRefresh: true);

                var projectsTask =
                    _firebase
                        .GetAllProjectsAsync();


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
                        .Where(project =>
                            !project.IsDeleted &&
                            project.CreatedBy ==
                                user.UniqueKey)
                        .Select(project =>
                            project.ProjectId)
                        .ToHashSet();


                // ═══════════════════════════════════════
                // REPAIR OLD / STUCK RETURN STATES
                //
                // Example:
                //
                // Request = Rejected
                // Tool    = PendingReturn
                //
                // This is the exact state that can make
                // the worker stay "Under Verification".
                // ═══════════════════════════════════════

                foreach (var result in returnRequests)
                {
                    var request =
                        result.Request;

                    if (request == null)
                        continue;

                    if (!myProjectIds.Contains(
                            request.ProjectId))
                    {
                        continue;
                    }

                    var tool =
                        allTools.FirstOrDefault(t =>
                            t.ToolId ==
                            request.ToolId);

                    if (tool == null)
                        continue;


                    // ───────────────────────────────────
                    // REJECTED REQUEST BUT TOOL IS STILL
                    // PENDING RETURN
                    // ───────────────────────────────────

                    if (request.Status ==
                            "Rejected" &&
                        tool.Status ==
                            "PendingReturn")
                    {
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

                        var repaired =
                            await _firebase
                                .UpdateToolAsync(tool);

                        if (repaired)
                        {
                            System.Diagnostics
                                .Debug
                                .WriteLine(
                                    $"Repaired rejected return: " +
                                    $"{tool.ToolId} -> Borrowed");
                        }
                    }
                }


                // Reload tools after possible repairs.
                allTools =
                    await _firebase.GetAllToolsAsync(
                        forceRefresh: true) ??
                    new List<Tool>();


                PendingReturnRequests.Clear();
                ProcessedReturnRequests.Clear();
                PendingCheckIns.Clear();


                // ═══════════════════════════════════════
                // PENDING CHECK-INS
                // ═══════════════════════════════════════

                var pendingCheckIns =
                    allTools
                        .Where(tool =>
                            tool.Status ==
                                "Borrowed" &&
                            tool.IsCheckInPending &&
                            myProjectIds.Contains(
                                tool.BorrowedProjectId))
                        .OrderByDescending(tool =>
                            tool.LastCheckInDate)
                        .ToList();


                foreach (var tool in pendingCheckIns)
                {
                    PendingCheckIns.Add(tool);
                }


                // ═══════════════════════════════════════
                // PENDING RETURNS
                //
                // IMPORTANT:
                //
                // We trust the RETURN REQUEST status here.
                //
                // We DO NOT hide it just because the Tool
                // status became out of sync.
                // ═══════════════════════════════════════

                var pendingReturns =
                    returnRequests
                        .Where(result =>
                        {
                            var request =
                                result.Request;

                            if (request == null)
                                return false;

                            if (request.Status !=
                                "Pending")
                            {
                                return false;
                            }

                            if (!myProjectIds.Contains(
                                    request.ProjectId))
                            {
                                return false;
                            }

                            return true;
                        })
                        .OrderByDescending(result =>
                            result.Request.RequestDate)
                        .ToList();


                foreach (var item in pendingReturns)
                {
                    PendingReturnRequests.Add(item);
                }


                // ═══════════════════════════════════════
                // PROCESSED RETURNS
                // ═══════════════════════════════════════

                var processedReturns =
                    returnRequests
                        .Where(result =>
                        {
                            var request =
                                result.Request;

                            if (request == null)
                                return false;

                            if (request.Status ==
                                "Pending")
                            {
                                return false;
                            }

                            return myProjectIds.Contains(
                                request.ProjectId);
                        })
                        .OrderByDescending(result =>
                            result.Request
                                .ReviewedDate ??
                            result.Request
                                .RequestDate)
                        .Take(10)
                        .ToList();


                foreach (var item in processedReturns)
                {
                    ProcessedReturnRequests.Add(item);
                }


                UpdateStats();

                OnPropertyChanged(
                    nameof(NoPendingReturn));

                OnPropertyChanged(
                    nameof(NoPendingCheckIns));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Load Return/Check-In error: " +
                    $"{ex.Message}");
            }
            finally
            {
                IsBusy = false;
                _isLoading = false;
            }
        }


        // ═══════════════════════════════════════════════
        // STATS
        // ═══════════════════════════════════════════════

        private void UpdateStats()
        {
            PendingCount =
                PendingReturnRequests.Count;

            ApprovedCount =
                ProcessedReturnRequests.Count(
                    result =>
                        result.Request.Status ==
                        "Approved");
        }


        // ═══════════════════════════════════════════════
        // REFRESH
        // ═══════════════════════════════════════════════

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


        // ═══════════════════════════════════════════════
        // VERIFY END-DAY CHECK-IN
        //
        // VERIFY:
        //      PE physically sees equipment.
        //
        // GOOD:
        //      remains Borrowed.
        //
        // DAMAGED:
        //      PE decides Minor/Major.
        //      Damage report created.
        //      Tool becomes Damaged.
        //
        // Worker/project remain attached because
        // this is NOT a formal return.
        // ═══════════════════════════════════════════════

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
                    "This equipment no longer has a " +
                    "pending end-day check-in.",
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


            if (string.IsNullOrWhiteSpace(
                    condition) ||
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
                    await Shell.Current
                        .DisplayActionSheet(
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
                    await Shell.Current
                        .DisplayPromptAsync(
                            "Damage Description",
                            "Describe the damage found " +
                            "during end-day inspection:",
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
                    : $"Condition: Damaged\n" +
                      $"Severity: {severity}";


            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Verify End-Day Check-In",
                    $"Confirm that you physically inspected " +
                    $"{tool.ToolName} ({tool.ToolId}).\n\n" +
                    $"Worker: {tool.AssignedWorkerName}\n" +
                    $"Project: {tool.BorrowedProjectName}\n" +
                    $"Location: {tool.LastCheckInLocation}\n\n" +
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
                        "Current Project Engineer could " +
                        "not be identified.",
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


                // ═══════════════════════════════════════
                // GOOD
                // ═══════════════════════════════════════

                if (condition == "Good")
                {
                    tool.IsCheckInPending =
                        false;

                    tool.LastCheckInVerifiedById =
                        user.UniqueKey;

                    tool.LastCheckInVerifiedByName =
                        user.FullName;

                    tool.Status =
                        "Borrowed";

                    tool.Condition =
                        "Good";


                    var updated =
                        await _firebase
                            .UpdateToolAsync(tool);


                    if (!updated)
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "Could not verify the " +
                            "end-day check-in.",
                            "OK");

                        return;
                    }


                    await _firebase
                        .LogTransactionAsync(
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
                                    $"Equipment physically " +
                                    $"verified in good condition " +
                                    $"at {location}.",

                                Condition =
                                    "Good",

                                Date =
                                    DateTime.Now
                            });


                    await Shell.Current.DisplayAlert(
                        "Check-In Verified",
                        $"{tool.ToolName} ({tool.ToolId}) " +
                        $"was verified.\n\n" +
                        $"Condition: Good\n" +
                        $"Location: {location}\n\n" +
                        $"The equipment remains assigned " +
                        $"to {workerName}.",
                        "OK");
                }


                // ═══════════════════════════════════════
                // DAMAGED
                // ═══════════════════════════════════════

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
                        await _firebase
                            .SubmitDamageReportAsync(
                                damageReport);


                    if (string.IsNullOrWhiteSpace(
                            reportKey))
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "Could not create the " +
                            "damage report.",
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


                    // DO NOT clear worker/project.
                    // End-day check-in is not a return.

                    var updated =
                        await _firebase
                            .UpdateToolAsync(tool);


                    if (!updated)
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "The damage report was created, " +
                            "but the equipment status could " +
                            "not be updated.",
                            "OK");

                        return;
                    }


                    await _firebase
                        .LogTransactionAsync(
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
                                    "Damage Found During Check-In",

                                Description =
                                    $"Damage discovered during " +
                                    $"end-day inspection at " +
                                    $"{location}. " +
                                    $"{severity} — " +
                                    $"{damageDescription}",

                                Condition =
                                    severity,

                                Date =
                                    DateTime.Now
                            });


                    await Shell.Current.DisplayAlert(
                        "Damage Found",
                        $"{tool.ToolName} ({tool.ToolId}) " +
                        $"was found damaged.\n\n" +
                        $"Severity: {severity}\n" +
                        $"Location: {location}\n\n" +
                        "A damage report has been created.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not verify check-in.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }


            await LoadAsync();
        }


        // ═══════════════════════════════════════════════
        // REJECT END-DAY CHECK-IN
        //
        // Equipment was NOT physically found/presented.
        //
        // No damage report.
        // Tool stays Borrowed.
        // Worker remains responsible.
        // Worker can check in again.
        // ═══════════════════════════════════════════════

        private async Task RejectCheckInAsync(
            Tool tool)
        {
            if (tool == null || IsBusy)
                return;


            if (!tool.IsCheckInPending ||
                tool.Status != "Borrowed")
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Check-In",
                    "This equipment no longer has a " +
                    "pending end-day check-in.",
                    "OK");

                await LoadAsync();

                return;
            }


            var reason =
                await Shell.Current.DisplayPromptAsync(
                    "Reject Check-In",
                    "Enter why the equipment could not " +
                    "be physically verified:",
                    "Continue",
                    "Cancel",
                    placeholder:
                        "e.g. Equipment was not at the reported location");


            if (string.IsNullOrWhiteSpace(
                    reason))
            {
                return;
            }


            reason =
                reason.Trim();


            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Reject Check-In",
                    $"Reject the check-in for " +
                    $"{tool.ToolName} ({tool.ToolId})?\n\n" +
                    $"Worker: {tool.AssignedWorkerName}\n" +
                    $"Reported Location: " +
                    $"{tool.LastCheckInLocation}\n\n" +
                    $"Reason: {reason}\n\n" +
                    "The equipment will remain assigned " +
                    "to the worker.",
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
                        "Current Project Engineer could " +
                        "not be identified.",
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

                string reportedLocation =
                    tool.LastCheckInLocation;


                // Clear only check-in request information.
                tool.IsCheckInPending =
                    false;

                tool.LastCheckInLocation =
                    string.Empty;

                tool.LastCheckInDate =
                    null;

                tool.LastCheckInVerifiedById =
                    string.Empty;

                tool.LastCheckInVerifiedByName =
                    string.Empty;


                // Still borrowed by same worker/project.
                tool.Status =
                    "Borrowed";


                var updated =
                    await _firebase
                        .UpdateToolAsync(tool);


                if (!updated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not reject the " +
                        "end-day check-in.",
                        "OK");

                    return;
                }


                await _firebase
                    .LogTransactionAsync(
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
                                "End Day Check-In Rejected",

                            Description =
                                $"Check-in rejected by " +
                                $"{user.FullName}. " +
                                $"Reported location: " +
                                $"{reportedLocation}. " +
                                $"Reason: {reason}",

                            Condition =
                                string.IsNullOrWhiteSpace(
                                    tool.Condition)
                                    ? "Good"
                                    : tool.Condition,

                            Date =
                                DateTime.Now
                        });


                await Shell.Current.DisplayAlert(
                    "Check-In Rejected",
                    $"{tool.ToolName} ({tool.ToolId}) " +
                    $"check-in was rejected.\n\n" +
                    $"Reason: {reason}\n\n" +
                    $"{workerName} remains responsible " +
                    $"for the equipment.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not reject check-in.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }


            await LoadAsync();
        }


        // ═══════════════════════════════════════════════
        // APPROVE / INSPECT RETURN
        // ═══════════════════════════════════════════════

        private async Task ApproveReturnAsync(
            ReturnRequestResult item)
        {
            if (item == null || IsBusy)
                return;


            var request =
                item.Request;


            if (request == null)
                return;


            if (request.Status != "Pending")
            {
                await Shell.Current.DisplayAlert(
                    "Already Processed",
                    "This return request has already " +
                    "been processed.",
                    "OK");

                await LoadAsync();

                return;
            }


            var condition =
                await Shell.Current.DisplayActionSheet(
                    "Return Inspection",
                    "Cancel",
                    null,
                    "Good",
                    "Damaged");


            if (string.IsNullOrWhiteSpace(
                    condition) ||
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
                    await Shell.Current
                        .DisplayActionSheet(
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
                    await Shell.Current
                        .DisplayPromptAsync(
                            "Damage Description",
                            "Describe the damage found " +
                            "during return inspection:",
                            "Continue",
                            "Cancel",
                            placeholder:
                                "e.g. Power cable damaged");


                if (string.IsNullOrWhiteSpace(
                        description))
                {
                    return;
                }


                damageDescription =
                    description.Trim();
            }


            string conditionDetails =
                condition == "Damaged"
                    ? $"Condition: Damaged\n" +
                      $"Severity: {severity}"
                    : "Condition: Good";


            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Inspect Return",
                    $"Confirm that you physically received " +
                    $"{request.ToolName} " +
                    $"({request.ToolId}).\n\n" +
                    $"Worker: {request.WorkerName}\n" +
                    $"Project: {request.ProjectName}\n\n" +
                    $"{conditionDetails}",
                    "Confirm Return",
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
                        "Current Project Engineer could " +
                        "not be identified.",
                        "OK");

                    return;
                }


                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            request.ToolId);


                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        $"Equipment {request.ToolId} " +
                        "could not be found.",
                        "OK");

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


                // ═══════════════════════════════════════
                // GOOD RETURN
                // ═══════════════════════════════════════

                if (condition == "Good")
                {
                    // Physical equipment first.
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
                        await _firebase
                            .UpdateToolAsync(tool);


                    if (!toolUpdated)
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "Could not update the equipment. " +
                            "The return request remains pending.",
                            "OK");

                        return;
                    }


                    request.Status =
                        "Approved";

                    request.VerifiedCondition =
                        "Good";

                    request.ReviewedDate =
                        DateTime.Now;

                    request.ReviewedById =
                        user.UniqueKey;

                    request.ReviewedByName =
                        user.FullName;


                    var requestUpdated =
                        await _firebase
                            .UpdateReturnRequestAsync(
                                item.Key,
                                request);


                    if (!requestUpdated)
                    {
                        await Shell.Current.DisplayAlert(
                            "Warning",
                            "The equipment was returned, " +
                            "but the request record could " +
                            "not be finalized.",
                            "OK");

                        return;
                    }


                    await _firebase
                        .LogTransactionAsync(
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
                                    $"Return physically inspected " +
                                    $"and approved by " +
                                    $"{user.FullName}. " +
                                    $"Equipment returned in " +
                                    $"good condition.",

                                Condition =
                                    "Good",

                                Date =
                                    DateTime.Now
                            });


                    await Shell.Current.DisplayAlert(
                        "Return Approved",
                        $"{tool.ToolName}\n" +
                        $"Equipment ID: {tool.ToolId}\n\n" +
                        "Condition: Good\n" +
                        "The equipment is now Available.",
                        "OK");
                }


                // ═══════════════════════════════════════
                // DAMAGED RETURN
                // ═══════════════════════════════════════

                else
                {
                    // Damage report retains historical
                    // worker/project accountability.

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
                        await _firebase
                            .SubmitDamageReportAsync(
                                damageReport);


                    if (string.IsNullOrWhiteSpace(
                            damageReportKey))
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "Could not create the damage " +
                            "report. Return was not finalized.",
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
                        await _firebase
                            .UpdateToolAsync(tool);


                    if (!toolUpdated)
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "Damage report was created, " +
                            "but equipment status could " +
                            "not be updated.",
                            "OK");

                        return;
                    }


                    request.Status =
                        "Approved";

                    request.VerifiedCondition =
                        severity;

                    request.ReviewedDate =
                        DateTime.Now;

                    request.ReviewedById =
                        user.UniqueKey;

                    request.ReviewedByName =
                        user.FullName;


                    var requestUpdated =
                        await _firebase
                            .UpdateReturnRequestAsync(
                                item.Key,
                                request);


                    if (!requestUpdated)
                    {
                        await Shell.Current.DisplayAlert(
                            "Warning",
                            "Damaged equipment was processed, " +
                            "but the return request record " +
                            "could not be finalized.",
                            "OK");

                        return;
                    }


                    await _firebase
                        .LogTransactionAsync(
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
                                    $"Return physically inspected " +
                                    $"by {user.FullName}. " +
                                    $"{severity}: " +
                                    $"{damageDescription}",

                                Condition =
                                    severity,

                                Date =
                                    DateTime.Now
                            });


                    await Shell.Current.DisplayAlert(
                        "Damaged Return Accepted",
                        $"{tool.ToolName}\n" +
                        $"Equipment ID: {tool.ToolId}\n\n" +
                        $"Assessment: {severity}\n" +
                        "A damage report has been created.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not process return.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }


            await LoadAsync();
        }


        // ═══════════════════════════════════════════════
        // REJECT RETURN
        //
        // Worker submitted return but equipment was
        // NOT physically returned / accepted.
        //
        // Request -> Rejected
        // Tool    -> Borrowed
        //
        // Worker remains responsible.
        // ═══════════════════════════════════════════════

        private async Task RejectReturnAsync(
            ReturnRequestResult item)
        {
            if (item == null || IsBusy)
                return;


            var request =
                item.Request;


            if (request == null)
                return;


            if (request.Status != "Pending")
            {
                await Shell.Current.DisplayAlert(
                    "Already Processed",
                    "This return request has already " +
                    "been processed.",
                    "OK");

                await LoadAsync();

                return;
            }


            var reason =
                await Shell.Current.DisplayPromptAsync(
                    "Reject Return",
                    "Enter why the physical return " +
                    "could not be completed:",
                    "Continue",
                    "Cancel",
                    placeholder:
                        "e.g. Equipment was not physically returned");


            if (string.IsNullOrWhiteSpace(
                    reason))
            {
                return;
            }


            reason =
                reason.Trim();


            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Reject Return",
                    $"Reject the return request for " +
                    $"{request.ToolName} " +
                    $"({request.ToolId})?\n\n" +
                    $"Worker: {request.WorkerName}\n" +
                    $"Project: {request.ProjectName}\n\n" +
                    $"Reason: {reason}\n\n" +
                    "The equipment will remain assigned " +
                    "to the worker.",
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
                        "Current Project Engineer could " +
                        "not be identified.",
                        "OK");

                    return;
                }


                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            request.ToolId);


                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        $"Equipment {request.ToolId} " +
                        "could not be found.",
                        "OK");

                    return;
                }


                // IMPORTANT:
                // Restore the physical tool FIRST.
                //
                // This prevents the worker from getting
                // stuck in PendingReturn if request status
                // changes before tool update.

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
                    await _firebase
                        .UpdateToolAsync(tool);


                if (!toolUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not restore the equipment " +
                        "to Borrowed. The return request " +
                        "was not rejected.",
                        "OK");

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

                request.Notes =
                    reason;


                var requestUpdated =
                    await _firebase
                        .UpdateReturnRequestAsync(
                            item.Key,
                            request);


                if (!requestUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Warning",
                        "The equipment was restored to " +
                        "Borrowed, but the return request " +
                        "record could not be finalized.",
                        "OK");

                    return;
                }


                await _firebase
                    .LogTransactionAsync(
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
                                $"Return rejected by " +
                                $"{user.FullName}. " +
                                $"Equipment ID: {tool.ToolId}. " +
                                $"Reason: {reason}. " +
                                $"Equipment remains assigned " +
                                $"to {request.WorkerName}.",

                            Condition =
                                string.IsNullOrWhiteSpace(
                                    tool.Condition)
                                    ? "Good"
                                    : tool.Condition,

                            Date =
                                DateTime.Now
                        });


                await Shell.Current.DisplayAlert(
                    "Return Rejected",
                    $"{request.ToolName}\n" +
                    $"Equipment ID: {request.ToolId}\n\n" +
                    $"Reason: {reason}\n\n" +
                    $"The equipment remains assigned to " +
                    $"{request.WorkerName}.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not reject return.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }


            await LoadAsync();
        }


        // ═══════════════════════════════════════════════
        // CLEAR CHECK-IN DATA
        // ═══════════════════════════════════════════════

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