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
    public class DamageReportsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService     _auth;
        private readonly ThemeService    _theme;
        // Add to existing fields at the top of the class:
        private List<TransactionLog> _allTransactions = new();
        private List<DamageReportResult> _allRawReports = new();

        // ── Theme ─────────────────────────────────────────────────
        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        // ── Stats ─────────────────────────────────────────────────
        private int _totalReports;
        public int TotalReports
        {
            get => _totalReports;
            private set => SetProperty(ref _totalReports, value);
        }

        private int _pendingReports;
        public int PendingReports
        {
            get => _pendingReports;
            private set => SetProperty(ref _pendingReports, value);
        }

        private int _resolvedReports;
        public int ResolvedReports
        {
            get => _resolvedReports;
            private set =>
                SetProperty(ref _resolvedReports, value);
        }

        // ── Collections ───────────────────────────────────────────
        public ObservableCollection<EnrichedDamageReportItem>
    Reports
        { get; } = new();


        // ── Empty State ───────────────────────────────────────────
        private bool _hasReports;
        public bool HasReports
        {
            get => _hasReports;
            private set
            {
                SetProperty(ref _hasReports, value);
                OnPropertyChanged(nameof(NoReports));
            }
        }
        public bool NoReports => !HasReports;

        // ── Filter ────────────────────────────────────────────────
        private string _selectedFilter = "All";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                SetProperty(ref _selectedFilter, value);
                MainThread.BeginInvokeOnMainThread(
                    async () => await LoadReportsAsync());
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
        public ICommand GoBackCommand      { get; }
        public ICommand RefreshCommand     { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand HandleReportCommand { get; }
        public ICommand SetFilterCommand   { get; }
        public ICommand DisputeReportCommand { get; }
        public ICommand AddNoteCommand { get; }

        // ── Constructor ───────────────────────────────────────────
        public DamageReportsViewModel(
            FirebaseService firebase,
            AuthService     auth,
            ThemeService    theme)
        {
            _firebase = firebase;
            _auth     = auth;
            _theme    = theme;
            Title     = "Damage Reports";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            GoBackCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            RefreshCommand = new Command(
                async () => await RefreshAsync());

            ToggleThemeCommand =
                new Command(() => _theme.Toggle());

            HandleReportCommand =
                new Command<DamageReportItem>(
                    async item =>
                        await HandleReportAsync(item));

            SetFilterCommand = new Command<string>(
                filter => SelectedFilter = filter ?? "All");

            MainThread.BeginInvokeOnMainThread(
                async () => await LoadReportsAsync());
            DisputeReportCommand = new Command<EnrichedDamageReportItem>(
    async item => await MarkDisputedAsync(item));

            AddNoteCommand = new Command<EnrichedDamageReportItem>(
                async item => await AddDisputeNoteAsync(item));
        }

        // ── Load Reports ──────────────────────────────────────────
        public async Task LoadReportsAsync()
        {
            IsBusy = true;
            try
            {
                // Load both in parallel
                var reportsTask = _firebase.GetAllDamageReportsRawAsync();
                var transactionsTask = _firebase.GetAllTransactionsAsync();
                await Task.WhenAll(reportsTask, transactionsTask);

                _allRawReports = reportsTask.Result ?? new();
                _allTransactions = transactionsTask.Result ?? new();

                TotalReports = _allRawReports.Count;
                PendingReports = _allRawReports.Count(r => r.Report.Status == "Pending");
                ResolvedReports = _allRawReports.Count(r => r.Report.Status == "Resolved");

                var filtered = SelectedFilter == "All"
                    ? _allRawReports
                    : _allRawReports.Where(r => r.Report.Status == SelectedFilter).ToList();

                Reports.Clear();
                foreach (var item in filtered.OrderByDescending(r => r.Report.ReportDate))
                {
                    var enriched = EnrichReport(item.Report, item.Key);
                    Reports.Add(enriched);
                }

                HasReports = Reports.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadReports error: {ex.Message}");
            }
            finally { IsBusy = false; }
        }


        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadReportsAsync();
            IsRefreshing = false;
        }

        // ── Handle Report ─────────────────────────────────────────
        private async Task HandleReportAsync(
            DamageReportItem item)
        {
            if (item is null || IsBusy) return;

            var action = await Shell.Current
                .DisplayActionSheet(
                    $"Handle Report — {item.ToolName}",
                    "Cancel", null,
                    "✅ Mark as Resolved",
                    "🔨 Send to Repair",
                    "🔧 Mark Under Maintenance",
                    "❌ Mark Tool as Lost");

            if (action == null ||
                action == "Cancel") return;

            IsBusy = true;
            try
            {
                // Update report status
                var newReportStatus = action switch
                {
                    "✅ Mark as Resolved"       => "Resolved",
                    "🔨 Send to Repair"         => "UnderRepair",
                    "🔧 Mark Under Maintenance" => "UnderRepair",
                    "❌ Mark Tool as Lost"      => "Lost",
                    _                           => item.Status
                };

                item.Report.Status = newReportStatus;
                await _firebase.UpdateDamageReportAsync(
                    item.ReportKey, item.Report);

                // Update tool status
                var tool = await _firebase
                    .GetToolByIdAsync(item.ToolId);

                if (tool != null)
                {
                    tool.Status = action switch
                    {
                        "✅ Mark as Resolved"       => "Available",
                        "🔨 Send to Repair"         => "UnderRepair",
                        "🔧 Mark Under Maintenance" => "UnderRepair",
                        "❌ Mark Tool as Lost"      => "Lost",
                        _                           => tool.Status
                    };

                    // Clear assignment if resolved or lost
                    if (tool.Status == "Available" ||
                        tool.Status == "Lost")
                    {
                        tool.AssignedWorkerId   = string.Empty;
                        tool.AssignedWorkerName = string.Empty;
                        tool.BorrowDate         = null;
                    }

                    await _firebase.UpdateToolAsync(tool);
                }

                await Shell.Current.DisplayAlert(
                    "✅ Report Updated",
                    $"{item.ToolName} ({item.ToolId}) " +
                    $"has been marked as {newReportStatus}.",
                    "OK");

                await LoadReportsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not update report.\n{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }
        private EnrichedDamageReportItem EnrichReport(
    DamageReport report, string key)
        {
            var item = new EnrichedDamageReportItem(report, key);

            // Get all transactions for this tool, newest first
            var toolTx = _allTransactions
                .Where(t => t.ToolId == report.ToolId)
                .OrderByDescending(t => t.Date)
                .ToList();

            // 1. Last handler = most recent borrow/transfer before report date
            var lastTx = toolTx
                .FirstOrDefault(t =>
                    t.Date <= report.ReportDate &&
                    (t.Action == "Borrowed" || t.Action == "Transferred"));

            if (lastTx != null)
            {
                item.LastHandlerName = lastTx.WorkerName;
                item.LastHandlerId = lastTx.WorkerId;
            }

            // 2. Primary custodian = whoever originally borrowed it
            var firstBorrow = toolTx
                .Where(t => t.Action == "Borrowed")
                .OrderBy(t => t.Date)
                .FirstOrDefault();

            item.PrimaryCustodian = firstBorrow?.WorkerName
                ?? report.WorkerName;

            // 3. Custody timeline (last 5 events before incident)
            item.CustodyTimeline = toolTx
                .Where(t => t.Date <= report.ReportDate)
                .Take(5)
                .Select(t => new CustodyEntry
                {
                    WorkerName = t.WorkerName,
                    Action = t.Action,
                    Date = t.Date
                })
                .ToList();

            // 4. Transfer count in 48h before incident
            var window = report.ReportDate.AddHours(-48);
            item.TransferCountRecent = toolTx
                .Count(t => t.Date >= window &&
                            t.Date <= report.ReportDate &&
                            t.Action == "Transferred");

            // 5. Prior incidents on same tool
            item.PriorIncidentCount = _allRawReports
                .Count(r => r.Report.ToolId == report.ToolId &&
                            r.Report.ReportDate < report.ReportDate);

            // 6. Confidence level
            bool hasLastHandler = !string.IsNullOrEmpty(item.LastHandlerId);
            bool hasCustodyData = item.CustodyTimeline.Count > 0;
            bool hasGoodDesc = report.Description?.Length > 20;

            item.ConfidenceLevel =
                (hasLastHandler && hasCustodyData && hasGoodDesc) ? "High" :
                (hasLastHandler || hasCustodyData) ? "Medium" :
                                                                    "Low";

            // 7. Dispute state from report model
            item.IsDisputed = report.IsDisputed;
            item.DisputeNoteCount = report.DisputeNotes?.Count ?? 0;

            return item;
        }
        private async Task MarkDisputedAsync(EnrichedDamageReportItem item)
        {
            if (item is null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Mark as Disputed",
                $"Mark this report for {item.ToolName} as disputed? " +
                "All involved workers will be able to add their statements.",
                "Yes, Mark Disputed", "Cancel");

            if (!confirm) return;

            item.Report.IsDisputed = true;
            await _firebase.UpdateDamageReportAsync(
                item.ReportKey, item.Report);
            await LoadReportsAsync();
        }

        private async Task AddDisputeNoteAsync(EnrichedDamageReportItem item)
        {
            if (item is null) return;

            // In a real flow you'd open a modal page.
            // For now use a prompt-style approach:
            string? note = await Shell.Current.DisplayPromptAsync(
                "Add Statement",
                "Describe your account of what happened with this tool:",
                "Submit", "Cancel",
                placeholder: "Enter your statement...",
                maxLength: 500);

            if (string.IsNullOrWhiteSpace(note)) return;

            var currentUser = _auth.CurrentUser;
            var workerId = currentUser?.UniqueKey ?? "unknown";

            item.Report.DisputeNotes ??= new Dictionary<string, string>();
            item.Report.DisputeNotes[workerId] = note.Trim();

            await _firebase.UpdateDamageReportAsync(
                item.ReportKey, item.Report);

            await Shell.Current.DisplayAlert(
                "Statement Recorded",
                "Your statement has been saved to this report.", "OK");

            await LoadReportsAsync();
        }
    }
}
