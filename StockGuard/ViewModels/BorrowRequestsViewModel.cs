using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    public class BorrowRequestsViewModel : BaseViewModel
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
        // COLLECTIONS
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<BorrowRequestItem>
            IncomingRequests
        { get; } = new();

        public ObservableCollection<BorrowRequestItem>
            OutgoingRequests
        { get; } = new();

        // ─────────────────────────────────────────────────────────
        // EMPTY STATES
        // ─────────────────────────────────────────────────────────

        private bool _hasIncoming;

        public bool HasIncoming
        {
            get => _hasIncoming;
            private set
            {
                SetProperty(
                    ref _hasIncoming,
                    value);

                OnPropertyChanged(
                    nameof(NoIncoming));
            }
        }

        public bool NoIncoming =>
            !HasIncoming;

        private bool _hasOutgoing;

        public bool HasOutgoing
        {
            get => _hasOutgoing;
            private set
            {
                SetProperty(
                    ref _hasOutgoing,
                    value);

                OnPropertyChanged(
                    nameof(NoOutgoing));
            }
        }

        public bool NoOutgoing =>
            !HasOutgoing;

        // ─────────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────────

        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set =>
                SetProperty(
                    ref _isRefreshing,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // COMMANDS
        // ─────────────────────────────────────────────────────────

        public ICommand GoBackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public ICommand AcceptCommand { get; }
        public ICommand DeclineCommand { get; }

        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public BorrowRequestsViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;

            Title = "Borrow Requests";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(
                    () =>
                        OnPropertyChanged(
                            nameof(ThemeIcon)));

            GoBackCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(".."));

            RefreshCommand =
                new Command(
                    async () =>
                        await RefreshAsync());

            ToggleThemeCommand =
                new Command(
                    () =>
                        _theme.Toggle());

            AcceptCommand =
                new Command<BorrowRequestItem>(
                    async item =>
                        await AcceptRequestAsync(
                            item));

            DeclineCommand =
                new Command<BorrowRequestItem>(
                    async item =>
                        await DeclineRequestAsync(
                            item));

            MainThread.BeginInvokeOnMainThread(
                async () =>
                    await LoadRequestsAsync());
        }

        // ─────────────────────────────────────────────────────────
        // LOAD REQUESTS
        // ─────────────────────────────────────────────────────────

        public async Task LoadRequestsAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var currentUser =
                    _auth.CurrentUser;

                if (currentUser == null)
                {
                    IncomingRequests.Clear();
                    OutgoingRequests.Clear();

                    HasIncoming = false;
                    HasOutgoing = false;

                    return;
                }

                var workerId =
     currentUser.UniqueKey;

                // ── TEMP DEBUG ─────────────────────────────────────
                System.Diagnostics.Debug.WriteLine(
                    "==========================================");

                System.Diagnostics.Debug.WriteLine(
                    "[BORROW DEBUG] LOAD REQUESTS");

                System.Diagnostics.Debug.WriteLine(
                    $"[BORROW DEBUG] Logged user: {currentUser.FullName}");

                System.Diagnostics.Debug.WriteLine(
                    $"[BORROW DEBUG] Logged workerId: '{workerId}'");

                System.Diagnostics.Debug.WriteLine(
                    "==========================================");

                var borrowTask =
                    _firebase
                        .GetAllBorrowRequestsRawAsync();

                var transferTask =
                    _firebase
                        .GetAllTransferRequestsRawAsync();

                await Task.WhenAll(
                    borrowTask,
                    transferTask);

                var allBorrowRequests =
     borrowTask.Result ??
     new List<BorrowRequestResult>();

                var allTransferRequests =
                    transferTask.Result ??
                    new List<TransferRequestResult>();

                // ── TEMP DEBUG ─────────────────────────────────────

                System.Diagnostics.Debug.WriteLine(
                    $"[BORROW DEBUG] Borrow requests loaded: " +
                    $"{allBorrowRequests.Count}");

                foreach (var result in allBorrowRequests)
                {
                    var request = result.Request;

                    System.Diagnostics.Debug.WriteLine(
                        "[BORROW DEBUG] --------------------------");

                    System.Diagnostics.Debug.WriteLine(
                        $"[BORROW DEBUG] Firebase Key: '{result.Key}'");

                    System.Diagnostics.Debug.WriteLine(
                        $"[BORROW DEBUG] Tool: '{request.ToolName}' " +
                        $"({request.ToolId})");

                    System.Diagnostics.Debug.WriteLine(
                        $"[BORROW DEBUG] Requester: " +
                        $"'{request.RequesterName}' " +
                        $"ID='{request.RequesterId}'");

                    System.Diagnostics.Debug.WriteLine(
                        $"[BORROW DEBUG] Owner: " +
                        $"'{request.OwnerName}' " +
                        $"ID='{request.OwnerId}'");

                    System.Diagnostics.Debug.WriteLine(
                        $"[BORROW DEBUG] Status: '{request.Status}'");

                    System.Diagnostics.Debug.WriteLine(
                        $"[BORROW DEBUG] Owner matches current worker: " +
                        $"{request.OwnerId == workerId}");

                    System.Diagnostics.Debug.WriteLine(
                        $"[BORROW DEBUG] Is pending: " +
                        $"{request.Status == "Pending"}");
                }

               

                // ─────────────────────────────────────────────
                // INCOMING
                // ─────────────────────────────────────────────

                IncomingRequests.Clear();

                var incomingBorrow =
                    allBorrowRequests
                        .Where(r =>
                            r.Request.OwnerId ==
                                workerId &&
                            r.Request.Status ==
                                "Pending")
                        .OrderByDescending(r =>
                            r.Request.RequestDate)
                        .ToList();
                System.Diagnostics.Debug.WriteLine(
                $"[BORROW DEBUG] Incoming borrow cards found: " +
                $"{incomingBorrow.Count}");

                foreach (var item in incomingBorrow)
                {
                    IncomingRequests.Add(
                        new BorrowRequestItem(
                            item.Request)
                        {
                            RequestKey =
                                item.Key
                        });
                }

                // ── Incoming transfers ───────────────────────

                var incomingTransfer =
                    allTransferRequests
                        .Where(r =>
                            r.Request.ToWorkerId ==
                                workerId &&
                            r.Request.Status ==
                                "Pending")
                        .OrderByDescending(r =>
                            r.Request.RequestDate)
                        .ToList();

                foreach (var item in incomingTransfer)
                {
                    IncomingRequests.Add(
                        CreateTransferDisplayItem(
                            item));
                }

                HasIncoming =
                    IncomingRequests.Count > 0;

                // ─────────────────────────────────────────────
                // OUTGOING
                // ─────────────────────────────────────────────

                OutgoingRequests.Clear();

                var outgoingBorrow =
                    allBorrowRequests
                        .Where(r =>
                            r.Request.RequesterId ==
                            workerId)
                        .OrderByDescending(r =>
                            r.Request.RequestDate)
                        .ToList();

                foreach (var item in outgoingBorrow)
                {
                    OutgoingRequests.Add(
                        new BorrowRequestItem(
                            item.Request)
                        {
                            RequestKey =
                                item.Key
                        });
                }

                // ── Outgoing transfers ───────────────────────

                var outgoingTransfer =
                    allTransferRequests
                        .Where(r =>
                            r.Request.FromWorkerId ==
                            workerId)
                        .OrderByDescending(r =>
                            r.Request.RequestDate)
                        .ToList();

                foreach (var item in outgoingTransfer)
                {
                    OutgoingRequests.Add(
                        CreateTransferDisplayItem(
                            item));
                }

                HasOutgoing =
                    OutgoingRequests.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadRequests error: " +
                    $"{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // TRANSFER DISPLAY ITEM
        // ─────────────────────────────────────────────────────────

        private static BorrowRequestItem
            CreateTransferDisplayItem(
                TransferRequestResult item)
        {
            return new BorrowRequestItem(
                new BorrowRequest
                {
                    ToolId =
                        item.Request.ToolId,

                    ToolName =
                        item.Request.ToolName,

                    RequesterId =
                        item.Request.FromWorkerId,

                    RequesterName =
                        item.Request.FromWorkerName,

                    OwnerId =
                        item.Request.ToWorkerId,

                    OwnerName =
                        item.Request.ToWorkerName,

                    Status =
                        item.Request.Status,

                    RequestDate =
                        item.Request.RequestDate
                })
            {
                RequestKey =
                    item.Key,

                IsTransfer =
                    true,

                TransferRequest =
                    item.Request
            };
        }

        // ─────────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────────

        private async Task RefreshAsync()
        {
            IsRefreshing = true;

            try
            {
                await LoadRequestsAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // ACCEPT
        // ─────────────────────────────────────────────────────────

        private async Task AcceptRequestAsync(
            BorrowRequestItem item)
        {
            if (item == null ||
                IsBusy)
            {
                return;
            }

            // Transfer and normal Borrow Request use
            // different workflows.
            if (item.IsTransfer)
            {
                await AcceptTransferAsync(
                    item);

                return;
            }

            await AcceptBorrowRequestAsync(
                item);
        }

        // ═════════════════════════════════════════════════════════
        // ACCEPT TRANSFER
        // ═════════════════════════════════════════════════════════

        private async Task AcceptTransferAsync(
            BorrowRequestItem item)
        {
            if (item.TransferRequest == null ||
                IsBusy)
            {
                return;
            }

            IsBusy = true;

            try
            {
                var currentUser =
                    _auth.CurrentUser;

                if (currentUser == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Your user session could not be identified.",
                        "OK");

                    return;
                }

                // ─────────────────────────────────────────────
                // GET LATEST REQUEST
                // ─────────────────────────────────────────────

                var allTransfers =
                    await _firebase
                        .GetAllTransferRequestsRawAsync();

                var latestResult =
                    allTransfers.FirstOrDefault(r =>
                        r.Key ==
                        item.RequestKey);

                if (latestResult == null ||
                    latestResult.Request == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Transfer Not Found",
                        "This transfer request no longer exists.",
                        "OK");

                    await LoadRequestsAsync();

                    return;
                }

                var transfer =
                    latestResult.Request;

                // ─────────────────────────────────────────────
                // REQUEST MUST STILL BE PENDING
                // ─────────────────────────────────────────────

                if (transfer.Status !=
                    "Pending")
                {
                    await Shell.Current.DisplayAlert(
                        "Already Processed",
                        "This transfer request has already " +
                        "been accepted or declined.",
                        "OK");

                    await LoadRequestsAsync();

                    return;
                }

                // ─────────────────────────────────────────────
                // CURRENT USER MUST BE RECEIVER
                // ─────────────────────────────────────────────

                if (transfer.ToWorkerId !=
                    currentUser.UniqueKey)
                {
                    await Shell.Current.DisplayAlert(
                        "Invalid Transfer",
                        "This transfer request is not assigned to you.",
                        "OK");

                    await LoadRequestsAsync();

                    return;
                }

                // ─────────────────────────────────────────────
                // LOAD ACTUAL TOOL BEFORE CHANGING REQUEST
                // ─────────────────────────────────────────────

                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            transfer.ToolId);

                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Tool Not Found",
                        "The equipment for this transfer could not be found.",
                        "OK");

                    return;
                }

                // ─────────────────────────────────────────────
                // TOOL MUST STILL BE BORROWED
                // ─────────────────────────────────────────────

                if (tool.Status !=
                    "Borrowed")
                {
                    await Shell.Current.DisplayAlert(
                        "Transfer Failed",
                        $"{tool.ToolName} is currently " +
                        $"{tool.Status} and can no longer " +
                        "be transferred.",
                        "OK");

                    return;
                }

                // ─────────────────────────────────────────────
                // SENDER MUST STILL OWN TOOL
                // ─────────────────────────────────────────────

                if (tool.AssignedWorkerId !=
                    transfer.FromWorkerId)
                {
                    await Shell.Current.DisplayAlert(
                        "Transfer Failed",
                        $"This equipment is no longer assigned " +
                        $"to {transfer.FromWorkerName}.\n\n" +
                        "The transfer cannot be completed.",
                        "OK");

                    return;
                }

                // ─────────────────────────────────────────────
                // DO NOT TRANSFER DURING PENDING END-DAY CHECK-IN
                // ─────────────────────────────────────────────

                if (tool.IsCheckInPending)
                {
                    await Shell.Current.DisplayAlert(
                        "Check-In Pending",
                        "This equipment has an End-Day Check-In " +
                        "waiting for Project Engineer verification.\n\n" +
                        "Complete the check-in before transferring it.",
                        "OK");

                    return;
                }

                // ─────────────────────────────────────────────
                // SAVE ACCOUNTABILITY DATA
                // ─────────────────────────────────────────────

                string previousWorkerId =
                    tool.AssignedWorkerId;

                string previousWorkerName =
                    tool.AssignedWorkerName;

                string projectId =
                    tool.BorrowedProjectId ??
                    string.Empty;

                string projectName =
                    tool.BorrowedProjectName ??
                    string.Empty;

                string condition =
                    string.IsNullOrWhiteSpace(
                        tool.Condition)
                        ? "Good"
                        : tool.Condition;

                // ─────────────────────────────────────────────
                // CONFIRM
                // ─────────────────────────────────────────────

                bool confirm =
                    await Shell.Current.DisplayAlert(
                        "Accept Transfer",
                        $"Accept transfer of " +
                        $"{tool.ToolName} ({tool.ToolId})?\n\n" +
                        $"From: {previousWorkerName}\n" +
                        $"To: {currentUser.FullName}\n" +
                        $"Project: {projectName}\n" +
                        $"Condition: {condition}",
                        "Accept",
                        "Cancel");

                if (!confirm)
                    return;

                // ─────────────────────────────────────────────
                // UPDATE TOOL FIRST
                // ─────────────────────────────────────────────

                tool.AssignedWorkerId =
                    currentUser.UniqueKey;

                tool.AssignedWorkerName =
                    currentUser.FullName;

                tool.BorrowDate =
                    DateTime.Now;

                // IMPORTANT:
                // Tool remains Borrowed.
                // Project remains unchanged.
                tool.Status =
                    "Borrowed";

                var toolUpdated =
                    await _firebase
                        .UpdateToolAsync(
                            tool);

                if (!toolUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Transfer Failed",
                        "The equipment custody could not be updated.",
                        "OK");

                    return;
                }

                // ─────────────────────────────────────────────
                // UPDATE TRANSFER REQUEST
                // ─────────────────────────────────────────────

                transfer.Status =
                    "Accepted";

                transfer.ProjectId =
                    projectId;

                transfer.ProjectName =
                    projectName;

                transfer.Condition =
                    condition;

                transfer.ReviewedDate =
                    DateTime.Now;

                transfer.ReviewedById =
                    currentUser.UniqueKey;

                transfer.ReviewedByName =
                    currentUser.FullName;

                var requestUpdated =
                    await _firebase
                        .UpdateTransferRequestAsync(
                            item.RequestKey,
                            transfer);

                if (!requestUpdated)
                {
                    // ── ROLLBACK TOOL ─────────────────────
                    //
                    // Avoid:
                    // Tool = Worker B
                    // Request = still Pending

                    tool.AssignedWorkerId =
                        previousWorkerId;

                    tool.AssignedWorkerName =
                        previousWorkerName;

                    await _firebase
                        .UpdateToolAsync(
                            tool);

                    await Shell.Current.DisplayAlert(
                        "Transfer Failed",
                        "The transfer request could not be updated. " +
                        "Equipment custody was restored to " +
                        $"{previousWorkerName}.",
                        "OK");

                    return;
                }

                // ─────────────────────────────────────────────
                // TRANSACTION
                // ─────────────────────────────────────────────

                await _firebase
                    .LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId =
                                tool.ToolId,

                            ToolName =
                                tool.ToolName,

                            // New responsible worker
                            WorkerId =
                                currentUser.UniqueKey,

                            WorkerName =
                                currentUser.FullName,

                            ProjectId =
                                projectId,

                            ProjectName =
                                projectName,

                            // Worker B accepted the transfer
                            PerformedById =
                                currentUser.UniqueKey,

                            PerformedByName =
                                currentUser.FullName,

                            Action =
                                "Transferred",

                            Description =
                                $"Equipment transferred from " +
                                $"{previousWorkerName} to " +
                                $"{currentUser.FullName}.",

                            Condition =
                                condition,

                            Date =
                                DateTime.Now
                        });

                await Shell.Current.DisplayAlert(
                    "Transfer Accepted",
                    $"{tool.ToolName} ({tool.ToolId}) " +
                    "has been transferred to you.\n\n" +
                    $"Project: {projectName}\n" +
                    $"Condition: {condition}",
                    "OK");

                await LoadRequestsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not accept transfer.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ═════════════════════════════════════════════════════════
        // ACCEPT BORROW REQUEST
        // ═════════════════════════════════════════════════════════

        private async Task AcceptBorrowRequestAsync(
            BorrowRequestItem item)
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var currentUser =
                    _auth.CurrentUser;

                if (currentUser == null)
                    return;

                // ─────────────────────────────────────────────
                // GET LATEST REQUEST
                // ─────────────────────────────────────────────

                var allBorrowRequests =
                    await _firebase
                        .GetAllBorrowRequestsRawAsync();

                var latestResult =
                    allBorrowRequests.FirstOrDefault(r =>
                        r.Key ==
                        item.RequestKey);

                if (latestResult == null ||
                    latestResult.Request == null ||
                    latestResult.Request.Status !=
                        "Pending")
                {
                    await Shell.Current.DisplayAlert(
                        "Already Processed",
                        "This borrow request has already " +
                        "been accepted or declined.",
                        "OK");

                    await LoadRequestsAsync();

                    return;
                }

                var request =
                    latestResult.Request;

                // Current worker must be the owner.
                if (request.OwnerId !=
                    currentUser.UniqueKey)
                {
                    await Shell.Current.DisplayAlert(
                        "Invalid Request",
                        "This borrow request is not assigned to you.",
                        "OK");

                    return;
                }

                // ─────────────────────────────────────────────
                // LOAD TOOL BEFORE APPROVING REQUEST
                // ─────────────────────────────────────────────

                var tool =
                    await _firebase
                        .GetToolByIdAsync(
                            request.ToolId);

                if (tool == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Tool Not Found",
                        "The equipment could not be found.",
                        "OK");

                    return;
                }

                // Owner must still hold it.
                if (tool.AssignedWorkerId !=
                    currentUser.UniqueKey ||
                    tool.Status !=
                    "Borrowed")
                {
                    await Shell.Current.DisplayAlert(
                        "Tool Unavailable",
                        $"{tool.ToolName} is no longer " +
                        "under your responsibility.",
                        "OK");

                    return;
                }

                if (tool.IsCheckInPending)
                {
                    await Shell.Current.DisplayAlert(
                        "Check-In Pending",
                        "This equipment has an End-Day Check-In " +
                        "waiting for verification and cannot be " +
                        "handed to another worker yet.",
                        "OK");

                    return;
                }

                string previousWorkerId =
                    tool.AssignedWorkerId;

                string previousWorkerName =
                    tool.AssignedWorkerName;

                string projectId =
                    tool.BorrowedProjectId ??
                    string.Empty;

                string projectName =
                    tool.BorrowedProjectName ??
                    string.Empty;

                bool confirm =
                    await Shell.Current.DisplayAlert(
                        "Accept Request",
                        $"Allow {request.RequesterName} to " +
                        $"borrow {tool.ToolName} " +
                        $"({tool.ToolId})?\n\n" +
                        $"Project: {projectName}",
                        "Accept",
                        "Cancel");

                if (!confirm)
                    return;

                // ─────────────────────────────────────────────
                // TRANSFER TOOL CUSTODY
                // ─────────────────────────────────────────────

                tool.AssignedWorkerId =
                    request.RequesterId;

                tool.AssignedWorkerName =
                    request.RequesterName;

                tool.BorrowDate =
                    DateTime.Now;

                tool.Status =
                    "Borrowed";

                var toolUpdated =
                    await _firebase
                        .UpdateToolAsync(
                            tool);

                if (!toolUpdated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not update equipment custody.",
                        "OK");

                    return;
                }

                // ─────────────────────────────────────────────
                // APPROVE REQUEST
                // ─────────────────────────────────────────────

                request.Status =
                    "Approved";

                var requestUpdated =
                    await _firebase
                        .UpdateBorrowRequestAsync(
                            item.RequestKey,
                            request);

                if (!requestUpdated)
                {
                    // Roll back custody.
                    tool.AssignedWorkerId =
                        previousWorkerId;

                    tool.AssignedWorkerName =
                        previousWorkerName;

                    await _firebase
                        .UpdateToolAsync(
                            tool);

                    await Shell.Current.DisplayAlert(
                        "Error",
                        "The borrow request could not be updated. " +
                        "Equipment custody was restored.",
                        "OK");

                    return;
                }

                // ─────────────────────────────────────────────
                // TRANSACTION
                // ─────────────────────────────────────────────

                await _firebase.LogTransactionAsync(
                    new TransactionLog
                    {
                        ToolId =
                            tool.ToolId,

                        ToolName =
                            tool.ToolName,

                        WorkerId =
                            request.RequesterId,

                        WorkerName =
                            request.RequesterName,

                        ProjectId =
                            projectId,

                        ProjectName =
                            projectName,

                        // Owner approved the handoff
                        PerformedById =
                            currentUser.UniqueKey,

                        PerformedByName =
                            currentUser.FullName,

                        Action =
                            "Borrowed",

                        Description =
                            $"Equipment handed from " +
                            $"{previousWorkerName} to " +
                            $"{request.RequesterName} " +
                            "through an approved borrow request.",

                        Condition =
                            tool.Condition,

                        Date =
                            DateTime.Now
                    });

                await Shell.Current.DisplayAlert(
                    "Request Accepted",
                    $"{tool.ToolName} has been handed to " +
                    $"{request.RequesterName}.",
                    "OK");

                await LoadRequestsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not accept request.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // DECLINE
        // ─────────────────────────────────────────────────────────

        private async Task DeclineRequestAsync(
            BorrowRequestItem item)
        {
            if (item == null ||
                IsBusy)
            {
                return;
            }

            if (item.IsTransfer)
            {
                await DeclineTransferAsync(
                    item);

                return;
            }

            await DeclineBorrowRequestAsync(
                item);
        }

        // ═════════════════════════════════════════════════════════
        // DECLINE TRANSFER
        // ═════════════════════════════════════════════════════════

        private async Task DeclineTransferAsync(
            BorrowRequestItem item)
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var currentUser =
                    _auth.CurrentUser;

                if (currentUser == null)
                    return;

                var allTransfers =
                    await _firebase
                        .GetAllTransferRequestsRawAsync();

                var latestResult =
                    allTransfers.FirstOrDefault(r =>
                        r.Key ==
                        item.RequestKey);

                if (latestResult == null ||
                    latestResult.Request == null ||
                    latestResult.Request.Status !=
                        "Pending")
                {
                    await Shell.Current.DisplayAlert(
                        "Already Processed",
                        "This transfer request has already " +
                        "been accepted or declined.",
                        "OK");

                    await LoadRequestsAsync();

                    return;
                }

                var transfer =
                    latestResult.Request;

                if (transfer.ToWorkerId !=
                    currentUser.UniqueKey)
                {
                    await Shell.Current.DisplayAlert(
                        "Invalid Transfer",
                        "This transfer request is not assigned to you.",
                        "OK");

                    return;
                }

                bool confirm =
                    await Shell.Current.DisplayAlert(
                        "Decline Transfer",
                        $"Decline transfer of " +
                        $"{transfer.ToolName} from " +
                        $"{transfer.FromWorkerName}?",
                        "Decline",
                        "Cancel");

                if (!confirm)
                    return;

                transfer.Status =
                    "Declined";

                transfer.ReviewedDate =
                    DateTime.Now;

                transfer.ReviewedById =
                    currentUser.UniqueKey;

                transfer.ReviewedByName =
                    currentUser.FullName;

                var updated =
                    await _firebase
                        .UpdateTransferRequestAsync(
                            item.RequestKey,
                            transfer);

                if (!updated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not decline the transfer request.",
                        "OK");

                    return;
                }

                // IMPORTANT:
                // Tool is NOT changed when transfer is declined.

                await Shell.Current.DisplayAlert(
                    "Transfer Declined",
                    $"You declined the transfer of " +
                    $"{transfer.ToolName}.",
                    "OK");

                await LoadRequestsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not decline transfer.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ═════════════════════════════════════════════════════════
        // DECLINE BORROW REQUEST
        // ═════════════════════════════════════════════════════════

        private async Task DeclineBorrowRequestAsync(
            BorrowRequestItem item)
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var currentUser =
                    _auth.CurrentUser;

                if (currentUser == null)
                    return;

                var allBorrow =
                    await _firebase
                        .GetAllBorrowRequestsRawAsync();

                var latestResult =
                    allBorrow.FirstOrDefault(r =>
                        r.Key ==
                        item.RequestKey);

                if (latestResult == null ||
                    latestResult.Request == null ||
                    latestResult.Request.Status !=
                        "Pending")
                {
                    await Shell.Current.DisplayAlert(
                        "Already Processed",
                        "This borrow request has already " +
                        "been accepted or declined.",
                        "OK");

                    await LoadRequestsAsync();

                    return;
                }

                var request =
                    latestResult.Request;

                if (request.OwnerId !=
                    currentUser.UniqueKey)
                {
                    await Shell.Current.DisplayAlert(
                        "Invalid Request",
                        "This borrow request is not assigned to you.",
                        "OK");

                    return;
                }

                bool confirm =
                    await Shell.Current.DisplayAlert(
                        "Decline Request",
                        $"Decline {request.RequesterName}'s " +
                        $"request for {request.ToolName}?",
                        "Decline",
                        "Cancel");

                if (!confirm)
                    return;

                request.Status =
                    "Declined";

                var updated =
                    await _firebase
                        .UpdateBorrowRequestAsync(
                            item.RequestKey,
                            request);

                if (!updated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not decline the borrow request.",
                        "OK");

                    return;
                }

                await Shell.Current.DisplayAlert(
                    "Request Declined",
                    $"You declined the request for " +
                    $"{request.ToolName}.",
                    "OK");

                await LoadRequestsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not decline request.\n" +
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