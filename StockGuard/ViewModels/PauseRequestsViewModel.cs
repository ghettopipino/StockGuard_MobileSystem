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
    public class PauseRequestsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        // ── Collections ───────────────────────────────────────────
        public ObservableCollection<PauseRequestItem>
            PendingRequests
        { get; } = new();

        public ObservableCollection<PauseRequestItem>
            ProcessedRequests
        { get; } = new();

        // ── Stats ─────────────────────────────────────────────────
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

        // ── Empty States ──────────────────────────────────────────
        private bool _hasPending;
        public bool HasPending
        {
            get => _hasPending;
            private set
            {
                SetProperty(ref _hasPending, value);
                OnPropertyChanged(nameof(NoPending));
            }
        }
        public bool NoPending => !HasPending;

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // ── Commands ──────────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }

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
                    OnPropertyChanged(
                        nameof(ThemeIcon)));

            GoBackCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            RefreshCommand = new Command(
                async () => await RefreshAsync());

            ToggleThemeCommand =
                new Command(() => _theme.Toggle());

            ApproveCommand =
                new Command<PauseRequestItem>(
                    async item =>
                        await ApproveAsync(item));

            RejectCommand =
                new Command<PauseRequestItem>(
                    async item =>
                        await RejectAsync(item));
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var all = await _firebase
                    .GetAllPauseRequestsRawAsync();

                PendingRequests.Clear();
                ProcessedRequests.Clear();

                var pending = all
                    .Where(r => r.Request.Status
                                == "Pending")
                    .OrderByDescending(r =>
                        r.Request.RequestDate)
                    .ToList();

                var processed = all
                    .Where(r => r.Request.Status
                                != "Pending")
                    .OrderByDescending(r =>
                        r.Request.RequestDate)
                    .Take(10)
                    .ToList();

                foreach (var item in pending)
                    PendingRequests.Add(
                        new PauseRequestItem(
                            item.Request, item.Key));

                foreach (var item in processed)
                    ProcessedRequests.Add(
                        new PauseRequestItem(
                            item.Request, item.Key));

                PendingCount = pending.Count;
                ApprovedCount = all
                    .Count(r => r.Request.Status
                                == "Approved");
                HasPending = PendingRequests.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadPauseRequests: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadAsync();
            IsRefreshing = false;
        }

        private async Task ApproveAsync(
    PauseRequestItem item)
        {
            if (item is null || IsBusy) return;

            // Step 1 — Confirm physical verification
            bool confirm =
                await Shell.Current.DisplayAlert(
                    "✅ Approve Pause",
                    $"Confirm you have physically " +
                    $"verified that {item.ToolName} " +
                    $"({item.ToolId}) is in the " +
                    $"project site storage?\n\n" +
                    $"Worker: {item.WorkerName}",
                    "Yes, Continue",
                    "Cancel");

            if (!confirm) return;

            // Step 2 — Select storage location
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
                    "📍 Where is the equipment located?",
                    "Cancel",
                    null,
                    locations);

            if (string.IsNullOrWhiteSpace(selectedLocation) ||
                selectedLocation == "Cancel")
                return;

            // Step 3 — Custom location
            if (selectedLocation == "Other")
            {
                selectedLocation =
                    await Shell.Current.DisplayPromptAsync(
                        "📍 Custom Location",
                        "Enter the exact equipment location:",
                        "Save",
                        "Cancel",
                        placeholder:
                            "e.g. Warehouse B - Storage Room 2");

                if (string.IsNullOrWhiteSpace(selectedLocation))
                    return;

                selectedLocation = selectedLocation.Trim();
            }

            IsBusy = true;

            try
            {
                var user = _auth.CurrentUser!;

                // Save location to pause request
                item.Request.HoldLocation =
                    selectedLocation;

                // Update pause request
                item.Request.Status = "Approved";
                item.Request.ApprovedDate = DateTime.Now;
                item.Request.ApprovedBy = user.FullName;

                await _firebase.UpdatePauseRequestAsync(
                    item.RequestKey,
                    item.Request);

                // Update tool status
                var tool = await _firebase
                    .GetToolByIdAsync(item.ToolId);

                if (tool != null)
                {
                    // Save OnHold information directly to the tool
                    tool.Status = "OnHold";

                    // Project holding the equipment
                    tool.HoldProjectId = item.Request.ProjectId;
                    tool.HoldProjectName = item.Request.ProjectName;

                    // Exact physical location
                    tool.HoldLocation = item.Request.HoldLocation;

                    // Preserve the worker who last borrowed it
                    tool.LastBorrowerId = item.Request.WorkerId;
                    tool.LastBorrowerName = item.Request.WorkerName;

                    // Record when it was placed on hold
                    tool.HoldDate = DateTime.Now;

                    // The worker is no longer actively borrowing it
                    tool.AssignedWorkerId = string.Empty;
                    tool.AssignedWorkerName = string.Empty;
                    tool.BorrowDate = null;

                    // The active borrowing project is no longer active custody
                    tool.BorrowedProjectId = string.Empty;
                    tool.BorrowedProjectName = string.Empty;

                    await _firebase.UpdateToolAsync(tool);

                    // Log transaction
                    await _firebase.LogTransactionAsync(
                        new TransactionLog
                        {
                            ToolId = tool.ToolId,
                            ToolName = tool.ToolName,
                            WorkerId = item.WorkerId,
                            WorkerName = item.WorkerName,
                            Action = "OnHold",
                            Description =
                                $"Pause approved by " +
                                $"{user.FullName}. " +
                                $"Tool stored at: " +
                                $"{selectedLocation}.",
                            Condition = tool.Condition,
                            Date = DateTime.Now
                        });
                }

                await Shell.Current.DisplayAlert(
                    "✅ Pause Approved",
                    $"{item.ToolName} is now On Hold.\n\n" +
                    $"📍 Location: {selectedLocation}\n" +
                    $"👤 Last borrower: {item.WorkerName}",
                    "OK");

                await LoadAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RejectAsync(
            PauseRequestItem item)
        {
            if (item is null || IsBusy) return;

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "❌ Reject Pause",
                    $"Reject pause request for " +
                    $"{item.ToolName}?\n\n" +
                    $"Tool will remain as Borrowed " +
                    $"under {item.WorkerName}.",
                    "Reject", "Cancel");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                item.Request.Status = "Rejected";
                await _firebase.UpdatePauseRequestAsync(
                    item.RequestKey, item.Request);

                // Revert tool to Borrowed
                var tool = await _firebase
                    .GetToolByIdAsync(item.ToolId);

                if (tool != null)
                {
                    tool.Status = "Borrowed";
                    await _firebase.UpdateToolAsync(tool);
                }

                await Shell.Current.DisplayAlert(
                    "Pause Rejected",
                    $"Pause request for " +
                    $"{item.ToolName} rejected.\n\n" +
                    $"Tool remains borrowed by " +
                    $"{item.WorkerName}.",
                    "OK");

                await LoadAsync();
            }
            finally { IsBusy = false; }
        }
    }
}   