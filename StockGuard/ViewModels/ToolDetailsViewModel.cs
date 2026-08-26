using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    /// <summary>
    /// Lazy-loading tool browser with forced catalog selection and pagination.
    ///
    /// Company-level stats use PHYSICAL tool status only:
    /// Total     = all non-deleted physical tools
    /// Available = Status == Available
    /// Borrowed  = Status == Borrowed or PendingReturn
    ///
    /// Project allocation is NOT treated as borrowing.
    /// </summary>
    public class ToolDetailsViewModel : BaseViewModel
    {
        // ─────────────────────────────────────────────────────
        // CONSTANTS
        // ─────────────────────────────────────────────────────

        private const int PageSize = 20;


        // ─────────────────────────────────────────────────────
        // DEPENDENCIES
        // ─────────────────────────────────────────────────────

        private readonly FirebaseService _firebase;
        private readonly ThemeService _theme;


        // ─────────────────────────────────────────────────────
        // RAW DATA
        // ─────────────────────────────────────────────────────

        private List<Tool> _allTools = new();
        private List<EquipmentCatalog> _allCatalogs = new();


        // ─────────────────────────────────────────────────────
        // PAGINATION
        // ─────────────────────────────────────────────────────

        private List<Tool> _filteredTools = new();
        private int _currentPage = 0;


        // ─────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────

        public string ThemeIcon =>
            _theme.IsDark
                ? "\uf185"
                : "\uf186";


        // ─────────────────────────────────────────────────────
        // COMPANY-LEVEL PHYSICAL INVENTORY STATS
        // ─────────────────────────────────────────────────────

        private int _totalTools;

        public int TotalTools
        {
            get => _totalTools;
            private set => SetProperty(
                ref _totalTools,
                value);
        }


        private int _availableTools;

        public int AvailableTools
        {
            get => _availableTools;
            private set => SetProperty(
                ref _availableTools,
                value);
        }


        private int _borrowedTools;

        public int BorrowedTools
        {
            get => _borrowedTools;
            private set => SetProperty(
                ref _borrowedTools,
                value);
        }


        // ─────────────────────────────────────────────────────
        // DISPLAYED TOOL LIST
        // ─────────────────────────────────────────────────────

        private ObservableCollection<Tool> _tools = new();

        public ObservableCollection<Tool> Tools
        {
            get => _tools;
            private set => SetProperty(
                ref _tools,
                value);
        }


        // ─────────────────────────────────────────────────────
        // CATALOG PICKER
        // ─────────────────────────────────────────────────────

        public ObservableCollection<EquipmentCatalog>
            Catalogs
        { get; } = new();


        // ─────────────────────────────────────────────────────
        // SEARCH
        // ─────────────────────────────────────────────────────

        private string _searchText = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(
                        ref _searchText,
                        value))
                {
                    ApplyFilters();
                }
            }
        }


        // ─────────────────────────────────────────────────────
        // CATALOG FILTER
        // ─────────────────────────────────────────────────────

        private EquipmentCatalog? _selectedCatalog;

        public EquipmentCatalog? SelectedCatalog
        {
            get => _selectedCatalog;
            set
            {
                if (SetProperty(
                        ref _selectedCatalog,
                        value))
                {
                    OnPropertyChanged(
                        nameof(
                            NoCatalogSelected));

                    ApplyFilters();
                }
            }
        }


        // ─────────────────────────────────────────────────────
        // STATUS FILTER
        // ─────────────────────────────────────────────────────

        private string _selectedStatus =
            string.Empty;

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(
                        ref _selectedStatus,
                        value))
                {
                    ApplyFilters();
                }
            }
        }


        public List<string> StatusOptions { get; } =
            new()
            {
                "All Status",
                "Available",
                "Borrowed",
                "Damaged",
                "UnderRepair",
                "PendingReturn",
                "Lost"
            };


        // ─────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────

        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(
                ref _isRefreshing,
                value);
        }


        // ─────────────────────────────────────────────────────
        // EMPTY / PAGINATION STATES
        // ─────────────────────────────────────────────────────

        public bool NoCatalogSelected =>
            _selectedCatalog == null ||
            string.IsNullOrWhiteSpace(
                _selectedCatalog.CatalogId);


        public bool NoTools =>
            Tools.Count == 0 &&
            !IsBusy &&
            !NoCatalogSelected;


        public bool HasMorePages =>
            (_currentPage + 1) *
            PageSize <
            _filteredTools.Count;


        // ─────────────────────────────────────────────────────
        // COMMANDS
        // ─────────────────────────────────────────────────────

        public ICommand RefreshCommand { get; }

        public ICommand ToggleThemeCommand { get; }

        public ICommand ShowQrCommand { get; }

        public ICommand ClearFiltersCommand { get; }

        public ICommand LoadMoreCommand { get; }


        // ─────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────

        public ToolDetailsViewModel(
            FirebaseService firebase,
            ThemeService theme)
        {
            _firebase = firebase;
            _theme = theme;

            Title = "Tools & QR Codes";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(
                    () =>
                    {
                        OnPropertyChanged(
                            nameof(ThemeIcon));
                    });


            RefreshCommand =
                new Command(
                    async () =>
                        await RefreshAsync());


            ToggleThemeCommand =
                new Command(
                    () => _theme.Toggle());


            ClearFiltersCommand =
                new Command(
                    ClearFilters);


            LoadMoreCommand =
                new Command(
                    LoadNextPage,
                    () => HasMorePages);


            ShowQrCommand =
                new Command<Tool>(
                    async tool =>
                    {
                        if (tool is null)
                            return;

                        await Shell.Current
                            .GoToAsync(
                                $"{nameof(QrDisplayView)}" +
                                $"?toolId=" +
                                $"{Uri.EscapeDataString(tool.ToolId)}" +
                                $"&toolName=" +
                                $"{Uri.EscapeDataString(tool.ToolName)}" +
                                $"&status=" +
                                $"{Uri.EscapeDataString(tool.Status)}" +
                                $"&catalogName=" +
                                $"{Uri.EscapeDataString(tool.ToolName)}");
                    });
        }


        // ─────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────

        public async Task LoadAsync(
            bool forceRefresh = false)
        {
            IsBusy = true;

            try
            {
                // ─────────────────────────────────────────────
                // LOAD TOOLS + CATALOGS
                // ─────────────────────────────────────────────

                var toolsTask =
                    _firebase
                        .GetAllToolsAsync(
                            forceRefresh);

                var catalogsTask =
                    _firebase
                        .GetAllCatalogsAsync(
                            forceRefresh);

                await Task.WhenAll(
                    toolsTask,
                    catalogsTask);


                _allTools =
                    (toolsTask.Result ??
                     new List<Tool>())
                    .Where(t =>
                        !t.IsDeleted)
                    .ToList();


                _allCatalogs =
                    (catalogsTask.Result ??
                     new List<EquipmentCatalog>())
                    .Where(c =>
                        !c.IsDeleted)
                    .ToList();


                // ─────────────────────────────────────────────
                // COMPANY-LEVEL PHYSICAL INVENTORY
                // ─────────────────────────────────────────────
                //
                // IMPORTANT:
                //
                // These numbers must mean the same thing as
                // Dashboard and Equipment Catalog.
                //
                // Allocation does NOT affect Available or
                // Borrowed.
                // ─────────────────────────────────────────────


                TotalTools =
                    _allTools.Count;


                AvailableTools =
                    _allTools.Count(t =>
                        string.Equals(
                            t.Status,
                            "Available",
                            StringComparison
                                .OrdinalIgnoreCase));


                BorrowedTools =
                    _allTools.Count(t =>
                        string.Equals(
                            t.Status,
                            "Borrowed",
                            StringComparison
                                .OrdinalIgnoreCase)
                        ||
                        string.Equals(
                            t.Status,
                            "PendingReturn",
                            StringComparison
                                .OrdinalIgnoreCase));


                // ─────────────────────────────────────────────
                // CATALOG PICKER
                // ─────────────────────────────────────────────

                bool isFirstLoad =
                    Catalogs.Count == 0;


                if (isFirstLoad ||
                    forceRefresh)
                {
                    var previouslySelectedId =
                        _selectedCatalog
                            ?.CatalogId;


                    Catalogs.Clear();


                    Catalogs.Add(
                        new EquipmentCatalog
                        {
                            CatalogId =
                                string.Empty,

                            CatalogName =
                                "Select a catalog..."
                        });


                    foreach (var catalog in
                             _allCatalogs
                                 .OrderBy(c =>
                                     c.CatalogName))
                    {
                        Catalogs.Add(
                            catalog);
                    }


                    if (isFirstLoad)
                    {
                        _selectedCatalog =
                            Catalogs[0];
                    }
                    else
                    {
                        _selectedCatalog =
                            Catalogs
                                .FirstOrDefault(
                                    c =>
                                        c.CatalogId ==
                                        previouslySelectedId)
                            ?? Catalogs[0];
                    }


                    OnPropertyChanged(
                        nameof(
                            SelectedCatalog));

                    OnPropertyChanged(
                        nameof(
                            NoCatalogSelected));
                }


                // ─────────────────────────────────────────────
                // TOOL LIST
                // ─────────────────────────────────────────────

                if (NoCatalogSelected)
                {
                    _filteredTools =
                        new List<Tool>();

                    Tools =
                        new ObservableCollection<Tool>();

                    _currentPage = 0;
                }
                else
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug
                    .WriteLine(
                        $"[ToolDetailsVM] " +
                        $"Load error: " +
                        $"{ex.Message}");
            }
            finally
            {
                IsBusy = false;

                NotifyPageStates();
            }
        }


        // ─────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────

        private async Task RefreshAsync()
        {
            IsRefreshing = true;

            _currentPage = 0;

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


        // ─────────────────────────────────────────────────────
        // FILTERS
        // ─────────────────────────────────────────────────────

        private void ApplyFilters()
        {
            if (NoCatalogSelected)
            {
                _filteredTools =
                    new List<Tool>();

                Tools =
                    new ObservableCollection<Tool>();

                _currentPage = 0;

                NotifyPageStates();

                return;
            }


            var filtered =
                _allTools
                    .AsEnumerable();


            // ─────────────────────────────────────────────
            // CATALOG
            // ─────────────────────────────────────────────

            filtered =
                filtered.Where(t =>
                    string.Equals(
                        t.CatalogId,
                        _selectedCatalog!
                            .CatalogId,
                        StringComparison
                            .OrdinalIgnoreCase));


            // ─────────────────────────────────────────────
            // SEARCH
            // ─────────────────────────────────────────────

            if (!string.IsNullOrWhiteSpace(
                    SearchText))
            {
                filtered =
                    filtered.Where(t =>
                        t.ToolId.Contains(
                            SearchText,
                            StringComparison
                                .OrdinalIgnoreCase)
                        ||
                        t.ToolName.Contains(
                            SearchText,
                            StringComparison
                                .OrdinalIgnoreCase));
            }


            // ─────────────────────────────────────────────
            // STATUS
            // ─────────────────────────────────────────────

            if (!string.IsNullOrWhiteSpace(
                    SelectedStatus)
                &&
                SelectedStatus !=
                    "All Status")
            {
                filtered =
                    filtered.Where(t =>
                        string.Equals(
                            t.Status,
                            SelectedStatus,
                            StringComparison
                                .OrdinalIgnoreCase));
            }


            _filteredTools =
                filtered
                    .OrderBy(t =>
                        t.ToolId)
                    .ToList();


            _currentPage = 0;


            Tools =
                new ObservableCollection<Tool>(
                    _filteredTools
                        .Take(PageSize));


            NotifyPageStates();
        }


        // ─────────────────────────────────────────────────────
        // LOAD NEXT PAGE
        // ─────────────────────────────────────────────────────

        private void LoadNextPage()
        {
            if (!HasMorePages)
                return;


            _currentPage++;


            var nextPage =
                _filteredTools
                    .Skip(
                        _currentPage *
                        PageSize)
                    .Take(
                        PageSize);


            foreach (var tool
                     in nextPage)
            {
                Tools.Add(tool);
            }


            NotifyPageStates();
        }


        // ─────────────────────────────────────────────────────
        // CLEAR FILTERS
        // ─────────────────────────────────────────────────────

        private void ClearFilters()
        {
            _searchText =
                string.Empty;

            _selectedStatus =
                string.Empty;


            _selectedCatalog =
                Catalogs
                    .FirstOrDefault();


            OnPropertyChanged(
                nameof(
                    SearchText));

            OnPropertyChanged(
                nameof(
                    SelectedStatus));

            OnPropertyChanged(
                nameof(
                    SelectedCatalog));

            OnPropertyChanged(
                nameof(
                    NoCatalogSelected));


            ApplyFilters();
        }


        // ─────────────────────────────────────────────────────
        // PAGE STATE NOTIFICATIONS
        // ─────────────────────────────────────────────────────

        private void NotifyPageStates()
        {
            OnPropertyChanged(
                nameof(
                    NoTools));

            OnPropertyChanged(
                nameof(
                    HasMorePages));

            OnPropertyChanged(
                nameof(
                    NoCatalogSelected));


            ((Command)
                LoadMoreCommand)
                .ChangeCanExecute();
        }
    }
}