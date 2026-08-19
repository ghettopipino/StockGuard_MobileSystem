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
    /// <summary>
    /// Admin-facing read-only tool detail page, reached by scanning a QR code.
    ///
    /// Shows:
    ///   - Tool identity (name, ID, status, condition)
    ///   - Current borrower info (if borrowed)
    ///   - Full transaction history (audit trail) for this tool
    ///
    /// No action buttons — admins use the sidebar pages (Damage Reports,
    /// Pause Requests, Worker Management) to take action.
    /// </summary>
    [QueryProperty(nameof(ToolId), "toolId")]
    public class AdminToolDetailsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
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
                        async () => await LoadAsync());
            }
        }

        // ── Tool Data ─────────────────────────────────────────────────────────
        private Tool? _tool;
        public Tool? Tool
        {
            get => _tool;
            private set
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
                OnPropertyChanged(nameof(CatalogName));
                OnPropertyChanged(nameof(IsBorrowed));
                OnPropertyChanged(nameof(IsAvailable));
                OnPropertyChanged(nameof(IsDamaged));
                OnPropertyChanged(nameof(ProjectName));
                OnPropertyChanged(nameof(AssignedByName));
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
        public bool IsBorrowed => Tool?.IsBorrowed ?? false;
        public bool IsAvailable => Tool?.IsAvailable ?? false;
        public bool IsDamaged => Tool?.IsDamaged ?? false;

        public string AssignedWorkerName =>
            string.IsNullOrEmpty(Tool?.AssignedWorkerName)
                ? "— Not assigned —"
                : Tool.AssignedWorkerName;

        public string BorrowDateDisplay =>
            Tool?.BorrowDate.HasValue == true
                ? Tool.BorrowDate.Value
                    .ToString("MMM d, yyyy h:mm tt")
                : "— Not borrowed —";

        public string ConditionText =>
            string.IsNullOrEmpty(Tool?.Condition)
                ? "Good"
                : Tool.Condition;
        public string ProjectName =>
            string.IsNullOrWhiteSpace(Tool?.BorrowedProjectName)
        ? "—"
        : Tool.BorrowedProjectName;

        public string AssignedByName =>
            string.IsNullOrWhiteSpace(Tool?.AssignedByName)
                ? "—"
                : Tool.AssignedByName;

        // ── Catalog name ──────────────────────────────────────────────────────
        private string _catalogName = string.Empty;
        public string CatalogName
        {
            get => _catalogName;
            private set => SetProperty(ref _catalogName, value);
        }

        // ── Transaction history (audit trail) ─────────────────────────────────
        public ObservableCollection<TransactionLog>
            Transactions
        { get; } = new();

        private bool _noTransactions;
        public bool NoTransactions
        {
            get => _noTransactions;
            private set => SetProperty(ref _noTransactions, value);
        }

        // ── Loading / not-found state ─────────────────────────────────────────
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
        public ICommand GoBackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────


        public AdminToolDetailsViewModel(
            FirebaseService firebase,
            ThemeService theme,
            AuthService auth)
        {
            _firebase = firebase;
            _theme = theme;
            Title = "Tool Details";
            _auth = auth;                   // ← ADD this line


            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            GoBackCommand = new Command(
                async () => await Shell.Current.GoToAsync(".."));
            RefreshCommand = new Command(
                async () => await LoadAsync());
            ToggleThemeCommand = new Command(
                () => _theme.Toggle());

        }

        // ── Load ──────────────────────────────────────────────────────────────
        public async Task LoadAsync()
        {
            if (string.IsNullOrEmpty(ToolId)) return;

            IsLoading = true;
            ToolNotFound = false;

            try
            {
                // Load tool and all transactions in parallel
                var toolTask = _firebase.GetToolByIdAsync(ToolId);
                var catalogsTask = _firebase.GetAllCatalogsAsync();
                var transactionsTask = _firebase.GetToolTransactionsAsync(ToolId);

                await Task.WhenAll(toolTask, catalogsTask, transactionsTask);

                var tool = toolTask.Result;
                var catalogs = catalogsTask.Result;
                var transactions = transactionsTask.Result;

                if (tool is null)
                {
                    ToolNotFound = true;
                    return;
                }

                Tool = tool;

                // Resolve catalog name
                var catalog = catalogs.FirstOrDefault(
                    c => c.CatalogId == tool.CatalogId);
                CatalogName = catalog?.CatalogName ?? "—";

                // Build transaction history newest-first
                Transactions.Clear();
                foreach (var tx in transactions
                    .OrderByDescending(t => t.Date))
                    Transactions.Add(tx);

                NoTransactions = Transactions.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AdminToolDetails] Load error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private readonly AuthService _auth;   // ← NEW field

        // ── Assign Worker gating ────────────────────────────────────────────────
     

       

    }
}
