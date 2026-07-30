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
    public class WorkerManagementViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        // ── Theme ─────────────────────────────────────────────────
        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        // ── Stats ─────────────────────────────────────────────────
        private int _totalWorkers;
        public int TotalWorkers
        {
            get => _totalWorkers;
            private set => SetProperty(ref _totalWorkers, value);
        }

        private int _activeWorkers;
        public int ActiveWorkers
        {
            get => _activeWorkers;
            private set => SetProperty(ref _activeWorkers, value);
        }

        private int _pendingWorkers;
        public int PendingWorkers
        {
            get => _pendingWorkers;
            private set
            {
                SetProperty(ref _pendingWorkers, value);
                OnPropertyChanged(nameof(HasPendingWorkers));
            }
        }

        public bool HasPendingWorkers => PendingWorkers > 0;

        // ── Collections ───────────────────────────────────────────
        public ObservableCollection<WorkerDisplayItem>
            ApprovedWorkers
        { get; } = new();

        public ObservableCollection<WorkerDisplayItem>
            PendingApprovals
        { get; } = new();

        // ── Empty States ──────────────────────────────────────────
        private bool _hasApproved;
        public bool HasApproved
        {
            get => _hasApproved;
            private set
            {
                SetProperty(ref _hasApproved, value);
                OnPropertyChanged(nameof(NoApproved));
            }
        }
        public bool NoApproved => !HasApproved;

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

        // ── Search ────────────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                MainThread.BeginInvokeOnMainThread(
                    async () => await LoadWorkersAsync());
            }
        }

        // ── Pull to Refresh ───────────────────────────────────────
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
        public ICommand ViewWorkerCommand { get; }
        public ICommand RemoveWorkerCommand { get; }

        // ── Constructor ───────────────────────────────────────────
        public WorkerManagementViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;
            Title = "Worker Management";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            GoBackCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            RefreshCommand = new Command(
                async () => await RefreshAsync());

            ToggleThemeCommand =
                new Command(() => _theme.Toggle());

            ApproveCommand =
                new Command<WorkerDisplayItem>(
                    async w => await ApproveWorkerAsync(w));

            RejectCommand =
                new Command<WorkerDisplayItem>(
                    async w => await RejectWorkerAsync(w));

            ViewWorkerCommand =
                new Command<WorkerDisplayItem>(
                    async w => await ViewWorkerDetailsAsync(w));

            RemoveWorkerCommand =
                new Command<WorkerDisplayItem>(
                    async w => await RemoveWorkerAsync(w));

            MainThread.BeginInvokeOnMainThread(
                async () => await LoadWorkersAsync());
        }

        // ── Load Workers ──────────────────────────────────────────
        public async Task LoadWorkersAsync()
        {
            IsBusy = true;
            try
            {
                var allUsers =
                    await _firebase.GetAllUsersAsync();

                // Filter workers only
                var workers = allUsers
                    .Where(u => u.Role == "Worker")
                    .ToList();

                // Apply search
                if (!string.IsNullOrWhiteSpace(SearchText))
                    workers = workers
                        .Where(w => w.FullName.Contains(
                            SearchText,
                            StringComparison.OrdinalIgnoreCase) ||
                            w.Email.Contains(
                            SearchText,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

                // Load tools for each worker
                var allTools =
                    await _firebase.GetAllToolsAsync();

                // ── Approved workers ─────────────────────────────
                ApprovedWorkers.Clear();
                var approved = workers
                    .Where(w => w.AccountStatus == "Approved")
                    .OrderBy(w => w.FullName)
                    .ToList();

                foreach (var worker in approved)
                {
                    var assignedTools = allTools
                        .Count(t => t.AssignedWorkerId ==
                                    worker.UniqueKey);

                    ApprovedWorkers.Add(
                        new WorkerDisplayItem(worker)
                        {
                            AssignedToolsCount = assignedTools
                        });
                }

                HasApproved = ApprovedWorkers.Count > 0;
                TotalWorkers = approved.Count;
                ActiveWorkers = approved
                    .Count(w => allTools.Any(t =>
                        t.AssignedWorkerId == w.UniqueKey));

                // ── Pending approval ─────────────────────────────
                PendingApprovals.Clear();
                var pending = workers
                    .Where(w => w.AccountStatus == "Pending")
                    .OrderByDescending(w => w.DateCreated)
                    .ToList();

                foreach (var worker in pending)
                    PendingApprovals.Add(
                        new WorkerDisplayItem(worker));

                HasPending = PendingApprovals.Count > 0;
                PendingWorkers = PendingApprovals.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadWorkers error: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadWorkersAsync();
            IsRefreshing = false;
        }

        // ── Approve Worker ────────────────────────────────────────
        private async Task ApproveWorkerAsync(
            WorkerDisplayItem item)
        {
            if (item is null || IsBusy) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Approve Worker",
                $"Approve {item.FullName} to access StockGuard?",
                "Approve", "Cancel");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                item.Worker.AccountStatus = "Approved";
                await _firebase.UpdateUserAsync(item.Worker);

                await Shell.Current.DisplayAlert(
                    "✅ Worker Approved",
                    $"{item.FullName} can now log in " +
                    $"to StockGuard.",
                    "OK");

                await LoadWorkersAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not approve worker.\n{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }

        // ── Reject Worker ─────────────────────────────────────────
        private async Task RejectWorkerAsync(
            WorkerDisplayItem item)
        {
            if (item is null || IsBusy) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Reject Worker",
                $"Reject {item.FullName}'s registration?",
                "Reject", "Cancel");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                item.Worker.AccountStatus = "Rejected";
                await _firebase.UpdateUserAsync(item.Worker);

                await Shell.Current.DisplayAlert(
                    "Worker Rejected",
                    $"{item.FullName}'s account has been rejected.",
                    "OK");

                await LoadWorkersAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not reject worker.\n{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }

        // ── View Worker Details ───────────────────────────────────
        private async Task ViewWorkerDetailsAsync(
            WorkerDisplayItem item)
        {
            if (item is null) return;

            var allTools =
                await _firebase.GetAllToolsAsync();

            var workerTools = allTools
                .Where(t => t.AssignedWorkerId ==
                            item.Worker.UniqueKey)
                .ToList();

            var toolList = workerTools.Count > 0
                ? string.Join("\n", workerTools.Select(t =>
                    $"  • {t.ToolName} ({t.ToolId})"))
                : "  No tools currently assigned";

            await Shell.Current.DisplayAlert(
                $"👷 {item.FullName}",
                $"Email: {item.Email}\n" +
                $"Phone Number: {item.PhoneNumber}\n" +
                $"Address: {item.Address}\n" +
                $"Status: {item.AccountStatus}\n" +
                $"Joined: {item.Worker.DateCreated:MMM d, yyyy}\n\n" +
                $"Assigned Tools ({workerTools.Count}):\n" +
                $"{toolList}",
                "Close");
        }

        // ── Remove Worker ─────────────────────────────────────────
        private async Task RemoveWorkerAsync(
            WorkerDisplayItem item)
        {
            if (item is null || IsBusy) return;

            // Check if worker has tools
            var allTools =
                await _firebase.GetAllToolsAsync();

            var workerTools = allTools
                .Where(t => t.AssignedWorkerId ==
                            item.Worker.UniqueKey)
                .ToList();

            if (workerTools.Count > 0)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Remove Worker",
                    $"{item.FullName} still has " +
                    $"{workerTools.Count} tool(s) assigned.\n\n" +
                    $"Please ensure all tools are returned " +
                    $"before removing this worker.",
                    "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Remove Worker",
                $"Remove {item.FullName} from StockGuard?\n\n" +
                $"This action cannot be undone.",
                "Remove", "Cancel");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                item.Worker.IsDeleted = true;
                await _firebase.UpdateUserAsync(item.Worker);

                await Shell.Current.DisplayAlert(
                    "Worker Removed",
                    $"{item.FullName} has been removed " +
                    $"from the system.",
                    "OK");

                await LoadWorkersAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not remove worker.\n{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }
    }
}
