using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    [QueryProperty(nameof(ToolId), "toolId")]
    [QueryProperty(nameof(ViewMode), "viewMode")]
    public class TransactionHistoryViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        // ── Pagination ────────────────────────────────────────────────────────
        private const int PageSize = 10;
        private int _currentPage = 1;

        private List<TransactionLog> _filteredTransactions = new();
        private List<TransactionLog> _allTransactions = new();

        // ── Query Properties ──────────────────────────────────────────────────
        private string _toolId = string.Empty;
        public string ToolId
        {
            get => _toolId;
            set
            {
                if (SetProperty(ref _toolId, value))
                {
                    OnPropertyChanged(nameof(PageSubtitle));
                    TryLoad(); // ← attempt load after this property arrives
                }
            }
        }

        private string _viewMode = string.Empty;
        public string ViewMode
        {
            get => _viewMode;
            set
            {
                if (SetProperty(ref _viewMode, value))
                {
                    OnPropertyChanged(nameof(PageTitle));
                    OnPropertyChanged(nameof(PageSubtitle));
                    OnPropertyChanged(nameof(IsWorkerMode));
                    OnPropertyChanged(nameof(IsAdminMode));
                    TryLoad(); // ← attempt load after this property arrives
                }
            }
        }
        private bool _loadRequested = false;
        private void TryLoad()
        {
            // ViewMode is always set (defaults to "worker" or "all" in constructor)
            // so we only truly need ToolId to be set for worker+tool mode.
            // But to be safe, wait until ViewMode is explicitly set by Shell too.
            if (string.IsNullOrEmpty(ViewMode)) return;

            // For worker mode viewing a specific tool, wait for ToolId too
            if (ViewMode == "worker" && string.IsNullOrEmpty(ToolId)) return;

            if (_loadRequested) return; // prevent double load
            _loadRequested = true;

            MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());
        }
        public void ResetLoadState()
        {
            _loadRequested = false;
        }

        public bool IsWorkerMode => ViewMode == "worker";
        public bool IsAdminMode => ViewMode != "worker";

        // ── Display ───────────────────────────────────────────────────────────
        public string ThemeIcon => _theme.IsDark ? "🌙" : "☀️";

        public string PageTitle => ViewMode switch
        {
            "tool" => "Tool History",
            "all" => "All Transactions",
            _ => "My Activity"
        };

        public string PageSubtitle => ViewMode switch
        {
            "tool" => $"All activity for {ToolId}",
            "all" => "Complete system transaction log",
            _ => "Your borrow and return history"
        };

        // ── Stats (always from full unfiltered list) ──────────────────────────
        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            private set => SetProperty(ref _totalCount, value);
        }

        private int _borrowCount;
        public int BorrowCount
        {
            get => _borrowCount;
            private set => SetProperty(ref _borrowCount, value);
        }

        private int _returnCount;
        public int ReturnCount
        {
            get => _returnCount;
            private set => SetProperty(ref _returnCount, value);
        }

        private int _damageCount;
        public int DamageCount
        {
            get => _damageCount;
            private set => SetProperty(ref _damageCount, value);
        }

        // ── NEW: Transfer count ───────────────────────────────────────────────
        private int _transferCount;
        public int TransferCount
        {
            get => _transferCount;
            private set => SetProperty(ref _transferCount, value);
        }

        // ── Visible transaction list ──────────────────────────────────────────
        public ObservableCollection<TransactionLog> Transactions { get; } = new();

        // ── Pagination state ──────────────────────────────────────────────────
        private bool _hasMoreItems;
        public bool HasMoreItems
        {
            get => _hasMoreItems;
            private set => SetProperty(ref _hasMoreItems, value);
        }

        private string _paginationLabel = string.Empty;
        public string PaginationLabel
        {
            get => _paginationLabel;
            private set => SetProperty(ref _paginationLabel, value);
        }

        private bool _isLoadingMore;
        public bool IsLoadingMore
        {
            get => _isLoadingMore;
            private set => SetProperty(ref _isLoadingMore, value);
        }

        // ── Empty / populated state ───────────────────────────────────────────
        private bool _hasTransactions;
        public bool HasTransactions
        {
            get => _hasTransactions;
            private set
            {
                SetProperty(ref _hasTransactions, value);
                OnPropertyChanged(nameof(NoTransactions));
            }
        }
        public bool NoTransactions => !HasTransactions && !IsBusy;

        // ── Filter ────────────────────────────────────────────────────────────
        private string _selectedFilter = "All";
        public string SelectedFilter
        {
            get => _selectedFilter;
            private set => SetProperty(ref _selectedFilter, value);
        }

        // ── Pull-to-refresh ───────────────────────────────────────────────────
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand SetFilterCommand { get; }
        public ICommand LoadMoreCommand { get; }
        public new ICommand GoBackCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public TransactionHistoryViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;

            ViewMode = auth.CurrentUser?.IsProjectEngineer == true
                ? "all"
                : "worker";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            GoBackCommand = new Command(async () =>
            {
                if (IsWorkerMode && !string.IsNullOrEmpty(ToolId))
                    await Shell.Current.GoToAsync(
                        $"//WorkerDashboardView/" +
                        $"{nameof(WorkerToolDetailsView)}" +
                        $"?toolId={Uri.EscapeDataString(ToolId)}");
                else
                    await Shell.Current.GoToAsync("..");
            });

            RefreshCommand = new Command(async () => await RefreshAsync());
            ToggleThemeCommand = new Command(() => _theme.Toggle());

            SetFilterCommand = new Command<string>(filter =>
            {
                var f = filter ?? "All";
                if (SelectedFilter == f) return;
                SelectedFilter = f;
                ApplyFilters();
            });

            LoadMoreCommand = new Command(
                execute: LoadNextPage,
                canExecute: () => HasMoreItems && !IsLoadingMore);
        }

        // ── Load ──────────────────────────────────────────────────────────────
        public async Task LoadAsync(bool forceRefresh = false)
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                var currentUser = _auth.CurrentUser;
                if (currentUser is null) return;

                if (ViewMode == "tool" &&
                    !string.IsNullOrEmpty(ToolId) &&
                    currentUser.IsProjectEngineer)
                {
                    _allTransactions = await _firebase
                        .GetToolTransactionsAsync(ToolId, forceRefresh);
                }
                else if (ViewMode == "all" && currentUser.IsProjectEngineer)
                {
                    _allTransactions = await _firebase
                        .GetAllTransactionsAsync(forceRefresh);
                }
                else
                {
                    _allTransactions = await _firebase
                        .GetWorkerTransactionsAsync(
                            currentUser.UniqueKey, forceRefresh);

                    if (!string.IsNullOrEmpty(ToolId))
                        _allTransactions = _allTransactions
                            .Where(t => t.ToolId == ToolId)
                            .ToList();
                }

                UpdateStats(_allTransactions);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[TransactionHistoryVM] Load error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(NoTransactions));
            }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadAsync(forceRefresh: true);
            IsRefreshing = false;
        }

        // ── Filter + pagination reset ─────────────────────────────────────────
        private void ApplyFilters()
        {
            var filtered = _allTransactions.AsEnumerable();

            if (SelectedFilter != "All")
                filtered = filtered.Where(t => t.Action == SelectedFilter);

            _filteredTransactions = filtered
                .OrderByDescending(t => t.Date)
                .ToList();

            _currentPage = 1;
            Transactions.Clear();

            foreach (var tx in _filteredTransactions.Take(PageSize))
                Transactions.Add(tx);

            HasTransactions = Transactions.Count > 0;
            UpdatePaginationState();
            OnPropertyChanged(nameof(NoTransactions));
        }

        // ── Load next page ────────────────────────────────────────────────────
        private void LoadNextPage()
        {
            if (!HasMoreItems || IsLoadingMore) return;

            IsLoadingMore = true;
            try
            {
                _currentPage++;

                var nextItems = _filteredTransactions
                    .Skip((_currentPage - 1) * PageSize)
                    .Take(PageSize);

                foreach (var tx in nextItems)
                    Transactions.Add(tx);

                UpdatePaginationState();
            }
            finally
            {
                IsLoadingMore = false;
                (LoadMoreCommand as Command)?.ChangeCanExecute();
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void UpdatePaginationState()
        {
            int visible = Transactions.Count;
            int total = _filteredTransactions.Count;

            HasMoreItems = visible < total;
            PaginationLabel = total == 0
                ? string.Empty
                : $"Showing {visible} of {total}";
        }

        private void UpdateStats(List<TransactionLog> all)
        {
            TotalCount = all.Count;
            BorrowCount = all.Count(t => t.Action == "Borrowed");
            ReturnCount = all.Count(t => t.Action == "Returned");
            DamageCount = all.Count(t => t.Action == "Damaged");
            TransferCount = all.Count(t => t.Action == "Transferred");
        }
    }
}