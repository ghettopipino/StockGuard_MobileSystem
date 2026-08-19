using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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

        // ═══════════════════════════════════════════════════════════
        // MODE / TAB
        // ═══════════════════════════════════════════════════════════

        private bool _isPauseMode = true;

        public bool IsPauseMode
        {
            get => _isPauseMode;
            private set
            {
                if (SetProperty(ref _isPauseMode, value))
                {
                    OnPropertyChanged(nameof(IsReturnMode));
                    UpdateStats();
                }
            }
        }

        public bool IsReturnMode => !IsPauseMode;

        // ═══════════════════════════════════════════════════════════
        // PAUSE COLLECTIONS
        // ═══════════════════════════════════════════════════════════

        public ObservableCollection<PauseRequestItem>
            PendingRequests
        { get; } = new();

        public ObservableCollection<PauseRequestItem>
            ProcessedRequests
        { get; } = new();

        // ═══════════════════════════════════════════════════════════
        // RETURN COLLECTIONS
        // ═══════════════════════════════════════════════════════════

        public ObservableCollection<ReturnRequestResult>
            PendingReturnRequests
        { get; } = new();

        public ObservableCollection<ReturnRequestResult>
            ProcessedReturnRequests
        { get; } = new();

        // ═══════════════════════════════════════════════════════════
        // STATS
        // ═══════════════════════════════════════════════════════════

        private int _pendingCount;

        public int PendingCount
        {
            get => _pendingCount;
            private set =>
                SetProperty(ref _pendingCount, value);
        }

        private int _approvedCount;

        public int ApprovedCount
        {
            get => _approvedCount;
            private set =>
                SetProperty(ref _approvedCount, value);
        }

        // ═══════════════════════════════════════════════════════════
        // EMPTY STATES
        // ═══════════════════════════════════════════════════════════

        public bool NoPendingPause =>
            PendingRequests.Count == 0;

        public bool NoPendingReturn =>
            PendingReturnRequests.Count == 0;

        // Keeps compatibility with your old XAML for now.
        public bool NoPending =>
            IsPauseMode
                ? NoPendingPause
                : NoPendingReturn;

        // ═══════════════════════════════════════════════════════════
        // REFRESH
        // ═══════════════════════════════════════════════════════════

        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(
                ref _isRefreshing,
                value);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════

        public ICommand OpenFlyoutCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public ICommand ShowPauseCommand { get; }
        public ICommand ShowReturnCommand { get; }

        // Pause
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }

        // Return
        public ICommand ApproveReturnCommand { get; }
        public ICommand RejectReturnCommand { get; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public PauseRequestsViewModel(
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
                new Command(() =>
                    _theme.Toggle());

            // ── Tabs ──────────────────────────────────────────

            ShowPauseCommand =
                new Command(() =>
                {
                    IsPauseMode = true;

                    OnPropertyChanged(nameof(NoPending));
                    OnPropertyChanged(nameof(NoPendingPause));
                    OnPropertyChanged(nameof(NoPendingReturn));
                });

            ShowReturnCommand =
                new Command(() =>
                {
                    IsPauseMode = false;

                    OnPropertyChanged(nameof(NoPending));
                    OnPropertyChanged(nameof(NoPendingPause));
                    OnPropertyChanged(nameof(NoPendingReturn));
                });

            // ── Pause Commands ────────────────────────────────

            ApproveCommand =
                new Command<PauseRequestItem>(
                    async item =>
                        await ApprovePauseAsync(item));

            RejectCommand =
                new Command<PauseRequestItem>(
                    async item =>
                        await RejectPauseAsync(item));

            // ── Return Commands ───────────────────────────────

            ApproveReturnCommand =
                new Command<ReturnRequestResult>(
                    async item =>
                        await ApproveReturnAsync(item));

            RejectReturnCommand =
                new Command<ReturnRequestResult>(
                    async item =>
                        await RejectReturnAsync(item));
        }

        // ═══════════════════════════════════════════════════════════
        // LOAD EVERYTHING
        // ═══════════════════════════════════════════════════════════

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                // Load both request types.
                var pauseRequests =
                    await _firebase
                        .GetAllPauseRequestsRawAsync();

                var returnRequests =
                    await _firebase
                        .GetAllReturnRequestsRawAsync();

                var allTools =
                await _firebase.GetAllToolsAsync(
                    forceRefresh: true);

                // ── Clear ─────────────────────────────────────

                PendingRequests.Clear();
                ProcessedRequests.Clear();

                PendingReturnRequests.Clear();
                ProcessedReturnRequests.Clear();

                // ═══════════════════════════════════════════════
                // PAUSE REQUESTS
                // ═══════════════════════════════════════════════

                var pendingPause =
                    pauseRequests
                        .Where(r =>
                            r.Request.Status == "Pending")
                        .OrderByDescending(r =>
                            r.Request.RequestDate)
                        .ToList();

                var processedPause =
                    pauseRequests
                        .Where(r =>
                            r.Request.Status != "Pending")
                        .OrderByDescending(r =>
                            r.Request.RequestDate)
                        .Take(10)
                        .ToList();

                foreach (var item in pendingPause)
                {
                    PendingRequests.Add(
                        new PauseRequestItem(
                            item.Request,
                            item.Key));
                }

                foreach (var item in processedPause)
                {
                    ProcessedRequests.Add(
                        new PauseRequestItem(
                            item.Request,
                            item.Key));
                }

                // ═══════════════════════════════════════════════
                // RETURN REQUESTS
                // ═══════════════════════════════════════════════

                var pendingReturn =
                 returnRequests
                     .Where(r =>
                     {
                         if (r.Request.Status != "Pending")
                             return false;

                         var tool = allTools.FirstOrDefault(t =>
                             t.ToolId == r.Request.ToolId);

                         // A return is only really pending when
                         // the physical tool is PendingReturn.
                         return tool != null &&
                                tool.Status == "PendingReturn";
                     })
                     .OrderByDescending(r =>
                         r.Request.RequestDate)
                     .ToList();

                var processedReturn =
                    returnRequests
                        .Where(r =>
                            r.Request.Status != "Pending")
                        .OrderByDescending(r =>
                            r.Request.RequestDate)
                        .Take(10)
                        .ToList();

                foreach (var item in pendingReturn)
                {
                    PendingReturnRequests.Add(item);
                }

                foreach (var item in processedReturn)
                {
                    ProcessedReturnRequests.Add(item);
                }

                UpdateStats();

                OnPropertyChanged(nameof(NoPending));
                OnPropertyChanged(nameof(NoPendingPause));
                OnPropertyChanged(nameof(NoPendingReturn));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Load Pause/Return Requests error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // UPDATE DYNAMIC STATS
        // ═══════════════════════════════════════════════════════════

        private void UpdateStats()
        {
            if (IsPauseMode)
            {
                PendingCount =
                    PendingRequests.Count;

                ApprovedCount =
                    ProcessedRequests.Count(r =>
                        r.Status == "Approved");
            }
            else
            {
                PendingCount =
                    PendingReturnRequests.Count;

                ApprovedCount =
                    ProcessedReturnRequests.Count(r =>
                        r.Request.Status == "Approved");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // REFRESH
        // ═══════════════════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════════════════
        // APPROVE PAUSE
        // ═══════════════════════════════════════════════════════════

        private async Task ApprovePauseAsync(
            PauseRequestItem item)
        {
            if (item is null || IsBusy)
                return;

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Approve Pause",
                    $"Confirm that you have physically verified " +
                    $"{item.ToolName} ({item.ToolId}) " +
                    $"at the project site storage.\n\n" +
                    $"Worker: {item.WorkerName}",
                    "Continue",
                    "Cancel");

            if (!confirm)
                return;

            string[] locations =
            {
                "Site Storage",
                "Warehouse",
                "Tool Room",
                "Worker Area",
                "Other"
            };

            string selectedLocation =
                await Shell.Current.DisplayActionSheet(
                    "Equipment Location",
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
                        "Custom Location",
                        "Enter the exact equipment location:",
                        "Save",
                        "Cancel",
                        placeholder:
                        "e.g. Warehouse B - Storage Room 2");

                if (string.IsNullOrWhiteSpace(
                        selectedLocation))
                {
                    return;
                }

                selectedLocation =
                    selectedLocation.Trim();
            }

            IsBusy = true;

            try
            {
                var user = _auth.CurrentUser;

                if (user == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Current user could not be identified.",
                        "OK");

                    return;
                }

                // ── Update Pause Request ──────────────────────

                item.Request.HoldLocation =
                    selectedLocation;

                item.Request.Status =
                    "Approved";

                item.Request.ApprovedDate =
                    DateTime.Now;

                item.Request.ApprovedBy =
                    user.FullName;

                var requestUpdated =
                    await _firebase
                        .UpdatePauseRequestAsync(
                            item.RequestKey,
                            item.Request);

                if (!requestUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not update the pause request.",
                        "OK");

                    return;
                }

                // ── Load Tool ─────────────────────────────────

                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            item.ToolId);

                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The tool could not be found.",
                        "OK");

                    return;
                }

                // ── Change to OnHold ──────────────────────────

                tool.Status = "OnHold";

                tool.HoldProjectId =
                    item.Request.ProjectId;

                tool.HoldProjectName =
                    item.Request.ProjectName;

                tool.HoldLocation =
                    selectedLocation;

                tool.HoldDate =
                    DateTime.Now;

                tool.LastBorrowerId =
                    item.Request.WorkerId;

                tool.LastBorrowerName =
                    item.Request.WorkerName;

                // Pause keeps accountability.
                tool.AssignedWorkerId =
                    item.Request.WorkerId;

                tool.AssignedWorkerName =
                    item.Request.WorkerName;

                tool.BorrowedProjectId =
                    item.Request.ProjectId;

                tool.BorrowedProjectName =
                    item.Request.ProjectName;

                // BorrowDate remains unchanged.

                var toolUpdated =
                    await _firebase
                        .UpdateToolAsync(tool);

                if (!toolUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Pause request was approved, but the tool status could not be updated.",
                        "OK");

                    return;
                }

                // ── Transaction ───────────────────────────────

                await _firebase
                    .LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId =
                                tool.ToolId,

                            ToolName =
                                tool.ToolName,

                            WorkerId =
                                item.Request.WorkerId,

                            WorkerName =
                                item.Request.WorkerName,

                            ProjectId =
                                item.Request.ProjectId,

                            ProjectName =
                                item.Request.ProjectName,

                            Action =
                                "OnHold",

                            Description =
                                $"Pause approved by " +
                                $"{user.FullName}. " +
                                $"Tool physically verified and stored at " +
                                $"{selectedLocation}.",

                            Condition =
                                tool.Condition,

                            Date =
                                DateTime.Now
                        });

                await Shell.Current.DisplayAlert(
                    "Pause Approved",
                    $"{item.ToolName} is now On Hold.\n\n" +
                    $"Location: {selectedLocation}\n" +
                    $"Responsible Worker: {item.WorkerName}",
                    "OK");

                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not approve pause request.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // REJECT PAUSE
        // ═══════════════════════════════════════════════════════════

        private async Task RejectPauseAsync(
            PauseRequestItem item)
        {
            if (item is null || IsBusy)
                return;

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Reject Pause",
                    $"Reject the pause request for " +
                    $"{item.ToolName} ({item.ToolId})?\n\n" +
                    $"The tool will remain Borrowed under " +
                    $"{item.WorkerName}.",
                    "Reject",
                    "Cancel");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                item.Request.Status =
                    "Rejected";

                var requestUpdated =
                    await _firebase
                        .UpdatePauseRequestAsync(
                            item.RequestKey,
                            item.Request);

                if (!requestUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not reject the pause request.",
                        "OK");

                    return;
                }

                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            item.ToolId);

                if (tool != null)
                {
                    tool.Status =
                        "Borrowed";

                    tool.AssignedWorkerId =
                        item.Request.WorkerId;

                    tool.AssignedWorkerName =
                        item.Request.WorkerName;

                    tool.BorrowedProjectId =
                        item.Request.ProjectId;

                    tool.BorrowedProjectName =
                        item.Request.ProjectName;

                    tool.HoldProjectId =
                        string.Empty;

                    tool.HoldProjectName =
                        string.Empty;

                    tool.HoldLocation =
                        string.Empty;

                    tool.HoldDate =
                        null;

                    await _firebase
                        .UpdateToolAsync(tool);
                }

                await Shell.Current.DisplayAlert(
                    "Pause Rejected",
                    $"{item.ToolName} remains borrowed by " +
                    $"{item.WorkerName}.",
                    "OK");

                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not reject pause request.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // APPROVE RETURN
        // ═══════════════════════════════════════════════════════════

        private async Task ApproveReturnAsync(
     ReturnRequestResult item)
        {
            if (item is null || IsBusy)
                return;

            var request = item.Request;

            // ── PE PHYSICAL INSPECTION ─────────────────────────────

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

            // ── IF DAMAGED ─────────────────────────────────────────

            if (condition == "Damaged")
            {
                var selectedSeverity =
                    await Shell.Current.DisplayActionSheet(
                        "Damage Severity",
                        "Cancel",
                        null,
                        "Minor Damage",
                        "Major Damage");

                if (string.IsNullOrWhiteSpace(selectedSeverity) ||
                    selectedSeverity == "Cancel")
                {
                    return;
                }

                severity = selectedSeverity;

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

            // ── CONFIRM ─────────────────────────────────────────────

            string conditionDetails =
                condition == "Damaged"
                    ? $"Condition: Damaged\n" +
                      $"Severity: {severity}"
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

                // ── SAVE ACCOUNTABILITY BEFORE CLEARING TOOL ───────

                string workerId =
                    request.WorkerId;

                string workerName =
                    request.WorkerName;

                string projectId =
                    request.ProjectId;

                string projectName =
                    request.ProjectName;

                // ── UPDATE RETURN REQUEST ───────────────────────────

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

                // ───────────────────────────────────────────────────
                // GOOD RETURN
                // ───────────────────────────────────────────────────

                if (condition == "Good")
                {
                    tool.Status =
                        "Available";

                    tool.Condition =
                        "Good";

                    // Physical return accepted.
                    // Worker/project custody ends here.
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

                            Action =
                                "Returned",

                            Description =
                                $"Return inspected and approved by " +
                                $"{user.FullName}. Equipment returned " +
                                $"in good condition.",

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

                // ───────────────────────────────────────────────────
                // DAMAGED RETURN
                // ───────────────────────────────────────────────────

                else
                {
                    // Create damage report BEFORE clearing
                    // accountability from the physical Tool.
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

                    if (string.IsNullOrEmpty(damageReportKey))
                    {
                        await Shell.Current.DisplayAlert(
                            "Error",
                            "The return was approved, but the damage report could not be created.",
                            "OK");

                        return;
                    }

                    // Returned physically, but NOT available
                    // for use because PE found damage.
                    tool.Status =
                        "Damaged";

                    tool.Condition =
                        severity;

                    // The DamageReport now preserves who had it
                    // and which project it came from.
                    //
                    // Physical custody has returned to company/PE.
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

        // ═══════════════════════════════════════════════════════════
        // REJECT RETURN
        // ═══════════════════════════════════════════════════════════

        private async Task RejectReturnAsync(
    ReturnRequestResult item)
        {
            if (item is null || IsBusy)
                return;

            var request = item.Request;

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
                        request.ToolId);

                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The equipment could not be found.",
                        "OK");

                    return;
                }

                // Reject is only valid while the return
                // is actually pending.
                if (tool.Status != "PendingReturn")
                {
                    await Shell.Current.DisplayAlert(
                        "Invalid Return",
                        "This equipment is no longer pending return.",
                        "OK");

                    await LoadAsync();
                    return;
                }

                // ── UPDATE REQUEST ──────────────────────────────────

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

                // ── RESTORE WORKER CUSTODY ─────────────────────────

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

     }
}