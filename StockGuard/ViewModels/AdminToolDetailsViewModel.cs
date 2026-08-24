using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    [QueryProperty(nameof(ToolId), "toolId")]
    public class AdminToolDetailsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly ThemeService _theme;
        private readonly AuthService _auth;

        // ─────────────────────────────────────────────────────────
        // QUERY
        // ─────────────────────────────────────────────────────────

        private string _toolId = string.Empty;

        public string ToolId
        {
            get => _toolId;
            set
            {
                SetProperty(ref _toolId, value);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    MainThread.BeginInvokeOnMainThread(
                        async () => await LoadAsync());
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // TOOL
        // ─────────────────────────────────────────────────────────

        private Tool? _tool;

        public Tool? Tool
        {
            get => _tool;
            private set
            {
                SetProperty(ref _tool, value);

                RefreshToolProperties();
            }
        }

        private void RefreshToolProperties()
        {
            OnPropertyChanged(nameof(ToolName));
            OnPropertyChanged(nameof(ToolIdDisplay));
            OnPropertyChanged(nameof(ToolIcon));

            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusIcon));

            OnPropertyChanged(nameof(AssignedWorkerName));
            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(AssignedByName));
            OnPropertyChanged(nameof(BorrowDateDisplay));
            OnPropertyChanged(nameof(ConditionText));

            OnPropertyChanged(nameof(IsBorrowed));
            OnPropertyChanged(nameof(IsAvailable));
            OnPropertyChanged(nameof(IsPendingReturn));
            OnPropertyChanged(nameof(IsDamaged));
            OnPropertyChanged(nameof(IsUnderRepair));
            OnPropertyChanged(nameof(IsLost));

            OnPropertyChanged(nameof(CheckInLocation));
            OnPropertyChanged(nameof(CheckInDateDisplay));
            OnPropertyChanged(nameof(CheckInVerifiedBy));
            OnPropertyChanged(nameof(HasCheckInInfo));
            OnPropertyChanged(nameof(IsCheckInPending));
        }

        // ─────────────────────────────────────────────────────────
        // DISPLAY
        // ─────────────────────────────────────────────────────────

        public string ToolName =>
            Tool?.ToolName ?? "Loading...";

        public string ToolIdDisplay =>
            Tool?.ToolId ?? string.Empty;

        public string ToolIcon =>
            Tool?.ToolIcon ?? "🔧";

        public string StatusText =>
            Tool?.Status ?? string.Empty;

        public string StatusColor =>
            Tool?.StatusColor ?? "#6b7280";

        public string StatusIcon =>
            Tool?.StatusIcon ?? "❓";

        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        public string AssignedWorkerName =>
            string.IsNullOrWhiteSpace(
                Tool?.AssignedWorkerName)
                ? "—"
                : Tool.AssignedWorkerName;

        public string ProjectName =>
            string.IsNullOrWhiteSpace(
                Tool?.BorrowedProjectName)
                ? "—"
                : Tool.BorrowedProjectName;

        public string AssignedByName =>
            string.IsNullOrWhiteSpace(
                Tool?.AssignedByName)
                ? "—"
                : Tool.AssignedByName;

        public string BorrowDateDisplay =>
            Tool?.BorrowDate.HasValue == true
                ? Tool.BorrowDate.Value
                    .ToString("MMM d, yyyy h:mm tt")
                : "—";

        public string ConditionText =>
            string.IsNullOrWhiteSpace(
                Tool?.Condition)
                ? "Good"
                : Tool.Condition;

        // ─────────────────────────────────────────────────────────
        // STATUS HELPERS
        // ─────────────────────────────────────────────────────────

        public bool IsBorrowed =>
            Tool?.IsBorrowed == true;

        public bool IsAvailable =>
            Tool?.IsAvailable == true;

        public bool IsPendingReturn =>
            Tool?.IsPendingReturn == true;

        public bool IsDamaged =>
            Tool?.IsDamaged == true;

        public bool IsUnderRepair =>
            Tool?.IsUnderRepair == true;

        public bool IsLost =>
            Tool?.IsLost == true;

        // ─────────────────────────────────────────────────────────
        // END-DAY CHECK-IN
        // ─────────────────────────────────────────────────────────

        public string CheckInLocation =>
            string.IsNullOrWhiteSpace(
                Tool?.LastCheckInLocation)
                ? "—"
                : Tool.LastCheckInLocation;

        public string CheckInDateDisplay =>
            Tool?.LastCheckInDate.HasValue == true
                ? Tool.LastCheckInDate.Value
                    .ToString("MMM d, yyyy h:mm tt")
                : "—";

        public string CheckInVerifiedBy =>
            string.IsNullOrWhiteSpace(
                Tool?.LastCheckInVerifiedByName)
                ? Tool?.IsCheckInPending == true
                    ? "Pending verification"
                    : "—"
                : Tool.LastCheckInVerifiedByName;

        public bool HasCheckInInfo =>
            Tool?.LastCheckInDate.HasValue == true ||
            Tool?.IsCheckInPending == true;

        public bool IsCheckInPending =>
            Tool?.IsCheckInPending == true;

        // ─────────────────────────────────────────────────────────
        // CATALOG
        // ─────────────────────────────────────────────────────────

        private string _catalogName =
            string.Empty;

        public string CatalogName
        {
            get => _catalogName;
            private set =>
                SetProperty(
                    ref _catalogName,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // TRANSACTIONS
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<TransactionLog>
            Transactions
        { get; } = new();

        private bool _noTransactions;

        public bool NoTransactions
        {
            get => _noTransactions;
            private set =>
                SetProperty(
                    ref _noTransactions,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // LOADING
        // ─────────────────────────────────────────────────────────

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(
                    ref _isLoading,
                    value);

                OnPropertyChanged(
                    nameof(IsNotLoading));
            }
        }

        public bool IsNotLoading =>
            !IsLoading;

        private bool _toolNotFound;

        public bool ToolNotFound
        {
            get => _toolNotFound;
            set =>
                SetProperty(
                    ref _toolNotFound,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // COMMANDS
        // ─────────────────────────────────────────────────────────

        public ICommand GoBackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public AdminToolDetailsViewModel(
            FirebaseService firebase,
            ThemeService theme,
            AuthService auth)
        {
            _firebase = firebase;
            _theme = theme;
            _auth = auth;

            Title = "Tool Details";

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
                        await LoadAsync());

            ToggleThemeCommand =
                new Command(
                    () => _theme.Toggle());
        }

        // ─────────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────────

        public async Task LoadAsync()
        {
            if (string.IsNullOrWhiteSpace(ToolId))
                return;

            IsLoading = true;
            ToolNotFound = false;

            try
            {
                var toolTask =
                    _firebase.GetToolByIdAsync(
                        ToolId);

                var catalogsTask =
                    _firebase.GetAllCatalogsAsync();

                var transactionsTask =
                    _firebase.GetToolTransactionsAsync(
                        ToolId,
                        forceRefresh: true);

                await Task.WhenAll(
                    toolTask,
                    catalogsTask,
                    transactionsTask);

                var tool =
                    toolTask.Result;

                var catalogs =
                    catalogsTask.Result ??
                    new List<EquipmentCatalog>();

                var transactions =
                    transactionsTask.Result ??
                    new List<TransactionLog>();

                if (tool == null)
                {
                    Tool = null;
                    ToolNotFound = true;
                    return;
                }

                Tool = tool;

                var catalog =
                    catalogs.FirstOrDefault(c =>
                        c.CatalogId ==
                        tool.CatalogId);

                CatalogName =
                    catalog?.CatalogName ??
                    "—";

                Transactions.Clear();

                foreach (var tx in
                    transactions
                        .OrderByDescending(t =>
                            t.Date))
                {
                    Transactions.Add(tx);
                }

                NoTransactions =
                    Transactions.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AdminToolDetails] Load error: {ex.Message}");

                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not load equipment details.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}