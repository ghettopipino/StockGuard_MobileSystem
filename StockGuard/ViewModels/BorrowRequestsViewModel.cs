using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        public ObservableCollection<BorrowRequestItem>
            IncomingRequests
        { get; } = new();

        public ObservableCollection<BorrowRequestItem>
            OutgoingRequests
        { get; } = new();

        private bool _hasIncoming;
        public bool HasIncoming
        {
            get => _hasIncoming;
            private set
            {
                SetProperty(ref _hasIncoming, value);
                OnPropertyChanged(nameof(NoIncoming));
            }
        }
        public bool NoIncoming => !HasIncoming;

        private bool _hasOutgoing;
        public bool HasOutgoing
        {
            get => _hasOutgoing;
            private set
            {
                SetProperty(ref _hasOutgoing, value);
                OnPropertyChanged(nameof(NoOutgoing));
            }
        }
        public bool NoOutgoing => !HasOutgoing;

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public ICommand GoBackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand DeclineCommand { get; }

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
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            GoBackCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            RefreshCommand = new Command(
                async () => await RefreshAsync());

            ToggleThemeCommand =
                new Command(() => _theme.Toggle());

            AcceptCommand =
                new Command<BorrowRequestItem>(
                    async item =>
                        await AcceptRequestAsync(item));

            DeclineCommand =
                new Command<BorrowRequestItem>(
                    async item =>
                        await DeclineRequestAsync(item));

            MainThread.BeginInvokeOnMainThread(async () =>
                await LoadRequestsAsync());
        }

        public async Task LoadRequestsAsync()
        {
            IsBusy = true;
            try
            {
                var currentUser = _auth.CurrentUser;
                if (currentUser is null) return;

                var workerId = currentUser.UniqueKey;

                var allBorrowRequests = await _firebase
                    .GetAllBorrowRequestsRawAsync();

                var allTransferRequests = await _firebase
                    .GetAllTransferRequestsRawAsync();

                // ── Incoming Borrow Requests ──────────────────
                // ✅ ONLY show Pending — remove once processed
                IncomingRequests.Clear();

                var incomingBorrow = allBorrowRequests
                    .Where(r =>
                        r.Request.OwnerId == workerId &&
                        r.Request.Status == "Pending") // ← Pending only
                    .OrderByDescending(r =>
                        r.Request.RequestDate)
                    .ToList();

                foreach (var item in incomingBorrow)
                {
                    IncomingRequests.Add(
                        new BorrowRequestItem(item.Request)
                        {
                            RequestKey = item.Key
                        });
                }

                // ── Incoming Transfer Requests ────────────────
                // ✅ ONLY show Pending transfers
                var incomingTransfer = allTransferRequests
                    .Where(r =>
                        r.Request.ToWorkerId == workerId &&
                        r.Request.Status == "Pending") // ← Pending only
                    .OrderByDescending(r =>
                        r.Request.RequestDate)
                    .ToList();

                foreach (var item in incomingTransfer)
                {
                    IncomingRequests.Add(
                        new BorrowRequestItem(
                            new BorrowRequest
                            {
                                ToolId = item.Request.ToolId,
                                ToolName = item.Request.ToolName,
                                RequesterId = item.Request.FromWorkerId,
                                RequesterName = item.Request.FromWorkerName,
                                OwnerId = item.Request.ToWorkerId,
                                Status = item.Request.Status,
                                RequestDate = item.Request.RequestDate
                            })
                        {
                            RequestKey = item.Key,
                            IsTransfer = true,
                            TransferRequest = item.Request
                        });
                }

                HasIncoming = IncomingRequests.Count > 0;

                // ── Outgoing Requests ─────────────────────────
                // ✅ Show ALL statuses so sender sees result
                OutgoingRequests.Clear();

                var outgoingBorrow = allBorrowRequests
                    .Where(r => r.Request.RequesterId == workerId)
                    .OrderByDescending(r =>
                        r.Request.RequestDate)
                    .ToList();

                foreach (var item in outgoingBorrow)
                {
                    OutgoingRequests.Add(
                        new BorrowRequestItem(item.Request)
                        {
                            RequestKey = item.Key
                        });
                }

                var outgoingTransfer = allTransferRequests
                    .Where(r => r.Request.FromWorkerId == workerId)
                    .OrderByDescending(r =>
                        r.Request.RequestDate)
                    .ToList();

                foreach (var item in outgoingTransfer)
                {
                    OutgoingRequests.Add(
                        new BorrowRequestItem(
                            new BorrowRequest
                            {
                                ToolId = item.Request.ToolId,
                                ToolName = item.Request.ToolName,
                                RequesterId = item.Request.FromWorkerId,
                                RequesterName = item.Request.FromWorkerName,
                                OwnerId = item.Request.ToWorkerId,
                                Status = item.Request.Status,
                                RequestDate = item.Request.RequestDate
                            })
                        {
                            RequestKey = item.Key,
                            IsTransfer = true,
                            TransferRequest = item.Request
                        });
                }

                HasOutgoing = OutgoingRequests.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadRequests error: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadRequestsAsync();
            IsRefreshing = false;
        }

        private async Task AcceptRequestAsync(
    BorrowRequestItem item)
        {
            if (item is null || IsBusy) return;
            IsBusy = true;

            try
            {
                // ── Handle Transfer Request ───────────────────
                if (item.IsTransfer &&
                    item.TransferRequest != null)
                {
                    // ✅ Check if transfer is still pending
                    // before processing
                    var allTransfers = await _firebase
                        .GetAllTransferRequestsRawAsync();

                    var latest = allTransfers.FirstOrDefault(
                        r => r.Key == item.RequestKey);

                    if (latest == null ||
                        latest.Request.Status != "Pending")
                    {
                        await Shell.Current.DisplayAlert(
                            "Already Processed",
                            "This transfer request has already " +
                            "been accepted or declined.",
                            "OK");
                        await LoadRequestsAsync();
                        return;
                    }

                    bool confirm = await Shell.Current
                        .DisplayAlert(
                            "Accept Transfer",
                            $"Accept {item.RequesterName}'s " +
                            $"transfer of {item.ToolName} " +
                            $"({item.ToolId}) to you?",
                            "Accept", "Cancel");

                    if (!confirm) return;

                    // Update transfer request
                    item.TransferRequest.Status = "Accepted";
                    await _firebase.UpdateTransferRequestAsync(
                        item.RequestKey, item.TransferRequest);

                    // Transfer tool to receiving worker
                    var tool = await _firebase
                        .GetToolByIdAsync(item.ToolId);

                    if (tool != null)
                    {
                        // ✅ Verify tool is still assigned
                        // to the sender before transferring
                        if (tool.AssignedWorkerId !=
                            item.TransferRequest.FromWorkerId)
                        {
                            await Shell.Current.DisplayAlert(
                                "Transfer Failed",
                                $"This tool is no longer assigned " +
                                $"to {item.TransferRequest.FromWorkerName}.\n\n" +
                                $"The transfer cannot be completed.",
                                "OK");
                            await LoadRequestsAsync();
                            return;
                        }

                        var prevWorker = tool.AssignedWorkerName;

                        tool.AssignedWorkerId =
                            item.TransferRequest.ToWorkerId;
                        tool.AssignedWorkerName =
                            item.TransferRequest.ToWorkerName;
                        tool.BorrowDate = DateTime.Now;

                        await _firebase.UpdateToolAsync(tool);

                        // Log transaction
                        await _firebase.LogTransactionAsync(
                            new TransactionLog
                            {
                                ToolId = tool.ToolId,
                                ToolName = tool.ToolName,
                                WorkerId =
                                    item.TransferRequest.ToWorkerId,
                                WorkerName =
                                    item.TransferRequest.ToWorkerName,
                                Action = "Transferred",
                                Description =
                                    $"Transferred from {prevWorker} " +
                                    $"to " +
                                    $"{item.TransferRequest.ToWorkerName}",
                                Condition = tool.Condition,
                                Date = DateTime.Now
                            });
                    }

                    await Shell.Current.DisplayAlert(
                        "✅ Transfer Accepted",
                        $"{item.ToolName} has been " +
                        $"transferred to you.",
                        "OK");

                    await LoadRequestsAsync();
                    return;
                }

                // ── Handle Borrow Request ─────────────────────
                // ✅ Check if borrow request is still pending
                var allBorrowRequests = await _firebase
                    .GetAllBorrowRequestsRawAsync();

                var latestBorrow = allBorrowRequests
                    .FirstOrDefault(r => r.Key == item.RequestKey);

                if (latestBorrow == null ||
                    latestBorrow.Request.Status != "Pending")
                {
                    await Shell.Current.DisplayAlert(
                        "Already Processed",
                        "This borrow request has already " +
                        "been accepted or declined.",
                        "OK");
                    await LoadRequestsAsync();
                    return;
                }

                bool confirmBorrow = await Shell.Current
                    .DisplayAlert(
                        "Accept Request",
                        $"Allow {item.RequesterName} to " +
                        $"borrow {item.ToolName} ({item.ToolId})?",
                        "Accept", "Cancel");

                if (!confirmBorrow) return;

                item.Request.Status = "Approved";
                await _firebase.UpdateBorrowRequestAsync(
                    item.RequestKey, item.Request);

                var borrowTool = await _firebase
                    .GetToolByIdAsync(item.ToolId);

                if (borrowTool != null)
                {
                    // ✅ Verify tool is still available
                    // before assigning
                    if (borrowTool.Status != "Available" &&
                        borrowTool.AssignedWorkerId !=
                        item.Request.OwnerId)
                    {
                        await Shell.Current.DisplayAlert(
                            "Tool Unavailable",
                            $"{item.ToolName} is no longer " +
                            $"available for borrowing.",
                            "OK");
                        await LoadRequestsAsync();
                        return;
                    }

                    borrowTool.AssignedWorkerId =
                        item.Request.RequesterId;
                    borrowTool.AssignedWorkerName =
                        item.Request.RequesterName;
                    borrowTool.BorrowDate = DateTime.Now;
                    borrowTool.Status = "Borrowed";

                    await _firebase.UpdateToolAsync(borrowTool);

                    await _firebase.LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId = borrowTool.ToolId,
                            ToolName = borrowTool.ToolName,
                            WorkerId = item.Request.RequesterId,
                            WorkerName = item.Request.RequesterName,
                            Action = "Borrowed",
                            Description =
                                $"Borrowed via request from " +
                                $"{_auth.CurrentUser?.FullName}",
                            Condition = borrowTool.Condition,
                            Date = DateTime.Now
                        });
                }

                await Shell.Current.DisplayAlert(
                    "✅ Request Accepted",
                    $"{item.ToolName} has been transferred " +
                    $"to {item.RequesterName}.",
                    "OK");

                await LoadRequestsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not accept request.\n{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }

        private async Task DeclineRequestAsync(
    BorrowRequestItem item)
        {
            if (item is null || IsBusy) return;
            IsBusy = true;

            try
            {
                // ✅ Check current status before declining
                if (item.IsTransfer &&
                    item.TransferRequest != null)
                {
                    var allTransfers = await _firebase
                        .GetAllTransferRequestsRawAsync();

                    var latest = allTransfers.FirstOrDefault(
                        r => r.Key == item.RequestKey);

                    if (latest == null ||
                        latest.Request.Status != "Pending")
                    {
                        await Shell.Current.DisplayAlert(
                            "Already Processed",
                            "This transfer request has already " +
                            "been accepted or declined.",
                            "OK");
                        await LoadRequestsAsync();
                        return;
                    }
                }
                else
                {
                    var allBorrow = await _firebase
                        .GetAllBorrowRequestsRawAsync();

                    var latest = allBorrow.FirstOrDefault(
                        r => r.Key == item.RequestKey);

                    if (latest == null ||
                        latest.Request.Status != "Pending")
                    {
                        await Shell.Current.DisplayAlert(
                            "Already Processed",
                            "This borrow request has already " +
                            "been accepted or declined.",
                            "OK");
                        await LoadRequestsAsync();
                        return;
                    }
                }

                bool confirm = await Shell.Current
                    .DisplayAlert(
                        item.IsTransfer
                            ? "Decline Transfer"
                            : "Decline Request",
                        item.IsTransfer
                            ? $"Decline transfer of " +
                              $"{item.ToolName} from " +
                              $"{item.RequesterName}?"
                            : $"Decline {item.RequesterName}'s " +
                              $"request for {item.ToolName}?",
                        "Decline", "Cancel");

                if (!confirm) return;

                if (item.IsTransfer &&
                    item.TransferRequest != null)
                {
                    item.TransferRequest.Status = "Declined";
                    await _firebase.UpdateTransferRequestAsync(
                        item.RequestKey,
                        item.TransferRequest);
                }
                else
                {
                    item.Request.Status = "Declined";
                    await _firebase.UpdateBorrowRequestAsync(
                        item.RequestKey, item.Request);
                }

                await Shell.Current.DisplayAlert(
                    "Request Declined",
                    $"You have declined the request " +
                    $"for {item.ToolName}.",
                    "OK");

                await LoadRequestsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not decline request.\n{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }
    }
}
