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

        // ─────────────────────────────────────────────────────────
        // PAGINATION
        // ─────────────────────────────────────────────────────────

        private const int PageSize = 10;

        private int _currentPage = 1;

        private List<TransactionLog>
            _allTransactions = new();

        private List<TransactionLog>
            _filteredTransactions = new();


        // ─────────────────────────────────────────────────────────
        // QUERY: TOOL
        // ─────────────────────────────────────────────────────────

        private string _toolId = string.Empty;

        public string ToolId
        {
            get => _toolId;
            set
            {
                if (SetProperty(ref _toolId, value))
                {
                    OnPropertyChanged(
                        nameof(PageSubtitle));

                    RequestLoad();
                }
            }
        }


        // ─────────────────────────────────────────────────────────
        // QUERY: VIEW MODE
        // ─────────────────────────────────────────────────────────
        //
        // worker = worker activity
        // tool   = PE viewing one tool
        // all    = PE viewing their project transactions
        //

        private string _viewMode = string.Empty;

        public string ViewMode
        {
            get => _viewMode;
            set
            {
                if (SetProperty(ref _viewMode, value))
                {
                    OnPropertyChanged(
                        nameof(PageTitle));

                    OnPropertyChanged(
                        nameof(PageSubtitle));

                    OnPropertyChanged(
                        nameof(IsWorkerMode));

                    OnPropertyChanged(
                        nameof(IsAdminMode));

                    RequestLoad();
                }
            }
        }


        private bool _loadRequested;

        private void RequestLoad()
        {
            if (string.IsNullOrWhiteSpace(ViewMode))
                return;

            if (ViewMode == "tool" &&
                string.IsNullOrWhiteSpace(ToolId))
            {
                return;
            }

            if (_loadRequested)
                return;

            _loadRequested = true;

            MainThread.BeginInvokeOnMainThread(
                async () =>
                    await LoadAsync());
        }

        public void ResetLoadState()
        {
            _loadRequested = false;
        }


        // ─────────────────────────────────────────────────────────
        // MODE HELPERS
        // ─────────────────────────────────────────────────────────

        public bool IsWorkerMode =>
            ViewMode == "worker";

        public bool IsAdminMode =>
            ViewMode != "worker";


        // ─────────────────────────────────────────────────────────
        // PAGE DISPLAY
        // ─────────────────────────────────────────────────────────

        public string ThemeIcon =>
            _theme.IsDark
                ? "🌙"
                : "☀️";

        public string PageTitle =>
            ViewMode switch
            {
                "tool" =>
                    "Tool History",

                "all" =>
                    "Transaction History",

                _ =>
                    "My Activity"
            };

        public string PageSubtitle =>
            ViewMode switch
            {
                "tool" =>
                    string.IsNullOrWhiteSpace(ToolId)
                        ? "Equipment activity"
                        : $"Activity for {ToolId}",

                "all" =>
                    "Activity from your managed projects",

                _ =>
                    string.IsNullOrWhiteSpace(ToolId)
                        ? "Your equipment activity"
                        : $"Your activity for {ToolId}"
            };


        // ─────────────────────────────────────────────────────────
        // STATS
        // ─────────────────────────────────────────────────────────

        private int _totalCount;

        public int TotalCount
        {
            get => _totalCount;
            private set =>
                SetProperty(
                    ref _totalCount,
                    value);
        }


        private int _borrowCount;

        public int BorrowCount
        {
            get => _borrowCount;
            private set =>
                SetProperty(
                    ref _borrowCount,
                    value);
        }


        private int _returnCount;

        public int ReturnCount
        {
            get => _returnCount;
            private set =>
                SetProperty(
                    ref _returnCount,
                    value);
        }


        private int _checkInCount;

        public int CheckInCount
        {
            get => _checkInCount;
            private set =>
                SetProperty(
                    ref _checkInCount,
                    value);
        }


        private int _damageCount;

        public int DamageCount
        {
            get => _damageCount;
            private set =>
                SetProperty(
                    ref _damageCount,
                    value);
        }


        // ─────────────────────────────────────────────────────────
        // TRANSACTIONS
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<TransactionLog>
            Transactions
        { get; } = new();


        // ─────────────────────────────────────────────────────────
        // PAGINATION STATE
        // ─────────────────────────────────────────────────────────

        private bool _hasMoreItems;

        public bool HasMoreItems
        {
            get => _hasMoreItems;
            private set
            {
                SetProperty(
                    ref _hasMoreItems,
                    value);

                (LoadMoreCommand as Command)?
                    .ChangeCanExecute();
            }
        }


        private string _paginationLabel =
            string.Empty;

        public string PaginationLabel
        {
            get => _paginationLabel;
            private set =>
                SetProperty(
                    ref _paginationLabel,
                    value);
        }


        private bool _isLoadingMore;

        public bool IsLoadingMore
        {
            get => _isLoadingMore;
            private set
            {
                SetProperty(
                    ref _isLoadingMore,
                    value);

                (LoadMoreCommand as Command)?
                    .ChangeCanExecute();
            }
        }


        // ─────────────────────────────────────────────────────────
        // EMPTY STATE
        // ─────────────────────────────────────────────────────────

        private bool _hasTransactions;

        public bool HasTransactions
        {
            get => _hasTransactions;
            private set
            {
                SetProperty(
                    ref _hasTransactions,
                    value);

                OnPropertyChanged(
                    nameof(NoTransactions));
            }
        }

        public bool NoTransactions =>
            !HasTransactions &&
            !IsBusy;


        // ─────────────────────────────────────────────────────────
        // FILTER
        // ─────────────────────────────────────────────────────────

        private string _selectedFilter =
            "All";

        public string SelectedFilter
        {
            get => _selectedFilter;
            private set =>
                SetProperty(
                    ref _selectedFilter,
                    value);
        }


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

        public ICommand OpenFlyoutCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand ToggleThemeCommand { get; }

        public ICommand SetFilterCommand { get; }

        public ICommand LoadMoreCommand { get; }

        public new ICommand GoBackCommand { get; }


        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public TransactionHistoryViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;

            // Set directly to avoid triggering
            // loading before constructor finishes.
            _viewMode =
                auth.CurrentUser?.IsProjectEngineer == true
                    ? "all"
                    : "worker";

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

            GoBackCommand =
                new Command(
                    async () =>
                    {
                        if (IsWorkerMode &&
                            !string.IsNullOrWhiteSpace(
                                ToolId))
                        {
                            await Shell.Current.GoToAsync(
                                $"//WorkerDashboardView/" +
                                $"{nameof(WorkerToolDetailsView)}" +
                                $"?toolId=" +
                                $"{Uri.EscapeDataString(ToolId)}");
                        }
                        else
                        {
                            await Shell.Current
                                .GoToAsync("..");
                        }
                    });

            RefreshCommand =
                new Command(
                    async () =>
                        await RefreshAsync());

            ToggleThemeCommand =
                new Command(
                    () => _theme.Toggle());

            SetFilterCommand =
                new Command<string>(
                    filter =>
                    {
                        string selected =
                            string.IsNullOrWhiteSpace(filter)
                                ? "All"
                                : filter;

                        if (SelectedFilter == selected)
                            return;

                        SelectedFilter =
                            selected;

                        ApplyFilters();
                    });

            LoadMoreCommand =
                new Command(
                    execute:
                        LoadNextPage,

                    canExecute:
                        () =>
                            HasMoreItems &&
                            !IsLoadingMore);

            MainThread.BeginInvokeOnMainThread(
                async () =>
                    await LoadAsync());
        }


        // ─────────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────────

        public async Task LoadAsync(
            bool forceRefresh = false)
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
                    _allTransactions.Clear();

                    Transactions.Clear();

                    UpdateStats(
                        _allTransactions);

                    HasTransactions =
                        false;

                    return;
                }


                // ───────────────────────────────────────────
                // PROJECT ENGINEER
                // ───────────────────────────────────────────

                if (currentUser.IsProjectEngineer)
                {
                    var projects =
                        await _firebase
                            .GetAllProjectsAsync();

                    var myProjectIds =
                        projects
                            .Where(p =>
                                !p.IsDeleted &&
                                p.CreatedBy ==
                                    currentUser.UniqueKey)
                            .Select(p =>
                                p.ProjectId)
                            .ToHashSet();


                    // ONE TOOL
                    if (ViewMode == "tool" &&
                        !string.IsNullOrWhiteSpace(
                            ToolId))
                    {
                        var toolTransactions =
                            await _firebase
                                .GetToolTransactionsAsync(
                                    ToolId,
                                    forceRefresh);

                        _allTransactions =
                            toolTransactions
                                .Where(t =>
                                    string.IsNullOrWhiteSpace(
                                        t.ProjectId) ||
                                    myProjectIds.Contains(
                                        t.ProjectId))
                                .ToList();
                    }

                    // ALL PE PROJECT TRANSACTIONS
                    else
                    {
                        var allTransactions =
                            await _firebase
                                .GetAllTransactionsAsync(
                                    forceRefresh);

                        _allTransactions =
                            allTransactions
                                .Where(t =>
                                    myProjectIds.Contains(
                                        t.ProjectId))
                                .ToList();
                    }
                }

                // ───────────────────────────────────────────
                // WORKER
                // ───────────────────────────────────────────

                else
                {
                    _allTransactions =
                        await _firebase
                            .GetWorkerTransactionsAsync(
                                currentUser.UniqueKey,
                                forceRefresh);

                    if (!string.IsNullOrWhiteSpace(
                            ToolId))
                    {
                        _allTransactions =
                            _allTransactions
                                .Where(t =>
                                    t.ToolId ==
                                    ToolId)
                                .ToList();
                    }
                }


                UpdateStats(
                    _allTransactions);

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[TransactionHistoryVM] Load error: " +
                    $"{ex.Message}");
            }
            finally
            {
                IsBusy = false;

                OnPropertyChanged(
                    nameof(NoTransactions));
            }
        }


        // ─────────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────────

        private async Task RefreshAsync()
        {
            IsRefreshing = true;

            try
            {
                await LoadAsync(
                    forceRefresh: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }


        // ─────────────────────────────────────────────────────────
        // FILTER
        // ─────────────────────────────────────────────────────────

        private void ApplyFilters()
        {
            IEnumerable<TransactionLog>
                filtered =
                    _allTransactions;

            switch (SelectedFilter)
            {
                case "Borrowed":

                    filtered =
                        filtered.Where(t =>
                            t.Action ==
                            "Borrowed");

                    break;


                case "Returned":

                    filtered =
                        filtered.Where(t =>
                            t.Action ==
                                "Returned" ||
                            t.Action ==
                                "Returned Damaged");

                    break;


                case "Check-In":

                    filtered =
                        filtered.Where(t =>
                            t.Action ==
                                "End Day Check-In" ||
                            t.Action ==
                                "End Day Check-In Verified");

                    break;


                case "Damage":

                    filtered =
                        filtered.Where(t =>
                            IsDamageAction(
                                t.Action));

                    break;
            }


            _filteredTransactions =
                filtered
                    .OrderByDescending(t =>
                        t.Date)
                    .ToList();


            _currentPage =
                1;

            Transactions.Clear();


            foreach (var transaction in
                _filteredTransactions
                    .Take(PageSize))
            {
                Transactions.Add(
                    transaction);
            }


            HasTransactions =
                Transactions.Count > 0;

            UpdatePaginationState();

            OnPropertyChanged(
                nameof(NoTransactions));
        }


        // ─────────────────────────────────────────────────────────
        // LOAD NEXT PAGE
        // ─────────────────────────────────────────────────────────

        private void LoadNextPage()
        {
            if (!HasMoreItems ||
                IsLoadingMore)
            {
                return;
            }

            IsLoadingMore =
                true;

            try
            {
                _currentPage++;

                var nextItems =
                    _filteredTransactions
                        .Skip(
                            (_currentPage - 1) *
                            PageSize)
                        .Take(PageSize)
                        .ToList();

                foreach (var transaction in nextItems)
                {
                    Transactions.Add(
                        transaction);
                }

                UpdatePaginationState();
            }
            finally
            {
                IsLoadingMore =
                    false;
            }
        }


        // ─────────────────────────────────────────────────────────
        // PAGINATION
        // ─────────────────────────────────────────────────────────

        private void UpdatePaginationState()
        {
            int visible =
                Transactions.Count;

            int total =
                _filteredTransactions.Count;

            HasMoreItems =
                visible < total;

            PaginationLabel =
                total == 0
                    ? string.Empty
                    : $"Showing {visible} of {total}";
        }


        // ─────────────────────────────────────────────────────────
        // STATS
        // ─────────────────────────────────────────────────────────

        private void UpdateStats(
            List<TransactionLog> all)
        {
            TotalCount =
                all.Count;

            BorrowCount =
                all.Count(t =>
                    t.Action ==
                    "Borrowed");

            ReturnCount =
                all.Count(t =>
                    t.Action ==
                        "Returned" ||
                    t.Action ==
                        "Returned Damaged");

            CheckInCount =
                all.Count(t =>
                    t.Action ==
                        "End Day Check-In" ||
                    t.Action ==
                        "End Day Check-In Verified");

            DamageCount =
                all.Count(t =>
                    IsDamageAction(
                        t.Action));
        }


        // ─────────────────────────────────────────────────────────
        // DAMAGE GROUP
        // ─────────────────────────────────────────────────────────

        private static bool IsDamageAction(
            string action)
        {
            return action ==
                       "Damage Reported" ||

                   action ==
                       "Damaged" ||

                   action ==
                       "Returned Damaged" ||

                   action ==
                       "UnderRepair" ||

                   action ==
                       "Resolved" ||

                   action ==
                       "Repaired" ||

                   action ==
                       "Lost";
        }
    }
}