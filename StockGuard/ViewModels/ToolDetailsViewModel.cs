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
    /// Key changes vs original:
    ///   1. No tools are rendered on first load — user must pick a catalog first.
    ///      This eliminates the "inflate 500 cards" lag on navigation.
    ///   2. Page-based pagination (PageSize = 20). Tools are loaded in slices;
    ///      a "Load More" command appends the next page into the ObservableCollection.
    ///   3. Search is fully functional: it filters across ALL tools in the selected
    ///      catalog (not just the current page), then resets to page 1.
    ///   4. Status filter works the same way — resets to page 1.
    ///   5. Pull-to-refresh forces a Firebase cache bypass and resets pagination.
    ///   6. NoCatalogSelected drives the "pick a catalog" empty state in the View.
    ///   7. HasMorePages drives the "Load More" button visibility in the View.
    /// </summary>
    public class ToolDetailsViewModel : BaseViewModel
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const int PageSize = 20;

        // ── Dependencies ─────────────────────────────────────────────────────
        private readonly FirebaseService _firebase;
        private readonly ThemeService _theme;

        // ── Raw data (never modified after load) ──────────────────────────────
        private List<Tool> _allTools = new();
        private List<EquipmentCatalog> _allCatalogs = new();

        // ── Pagination state ─────────────────────────────────────────────────
        // _filteredTools holds the full result of the current filter pass.
        // Tools is the *paged slice* shown in the CollectionView.
        private List<Tool> _filteredTools = new();
        private int _currentPage = 0;

        // ── Theme ─────────────────────────────────────────────────────────────
        public string ThemeIcon => _theme.IsDark ? "🌙" : "☀️";

        // ── Stats ─────────────────────────────────────────────────────────────
        private int _totalTools;
        public int TotalTools
        {
            get => _totalTools;
            private set => SetProperty(ref _totalTools, value);
        }

        private int _availableTools;
        public int AvailableTools
        {
            get => _availableTools;
            private set => SetProperty(ref _availableTools, value);
        }

        private int _borrowedTools;
        public int BorrowedTools
        {
            get => _borrowedTools;
            private set => SetProperty(ref _borrowedTools, value);
        }

        // ── Displayed (paged) tool list ───────────────────────────────────────
        private ObservableCollection<Tool> _tools = new();
        public ObservableCollection<Tool> Tools
        {
            get => _tools;
            private set => SetProperty(ref _tools, value);
        }

        // ── Catalog picker ────────────────────────────────────────────────────
        public ObservableCollection<EquipmentCatalog> Catalogs { get; } = new();

        // ── Search ────────────────────────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilters();
            }
        }

        // ── Catalog filter ────────────────────────────────────────────────────
        private EquipmentCatalog? _selectedCatalog;
        public EquipmentCatalog? SelectedCatalog
        {
            get => _selectedCatalog;
            set
            {
                if (SetProperty(ref _selectedCatalog, value))
                {
                    OnPropertyChanged(nameof(NoCatalogSelected));
                    ApplyFilters();
                }
            }
        }

        // ── Status filter ─────────────────────────────────────────────────────
        private string _selectedStatus = string.Empty;
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                    ApplyFilters();
            }
        }

        public List<string> StatusOptions { get; } =
            new() { "All Status", "Available", "Borrowed", "Damaged" };

        // ── Pull-to-refresh ───────────────────────────────────────────────────
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // ── Empty / pagination states ─────────────────────────────────────────

        /// <summary>
        /// True while no catalog has been chosen yet (or the placeholder
        /// "Pick a catalog" item is selected). Drives the "select a catalog first"
        /// empty state in the View.
        /// </summary>
        public bool NoCatalogSelected =>
            _selectedCatalog == null || string.IsNullOrEmpty(_selectedCatalog.CatalogId);

        /// <summary>
        /// True when the filtered list is empty AND we are not loading AND a real
        /// catalog is selected. Drives the "No tools found" card.
        /// </summary>
        public bool NoTools =>
            Tools.Count == 0 && !IsBusy && !NoCatalogSelected;

        /// <summary>
        /// True when there are more pages of filtered results beyond what is
        /// currently shown. Drives the "Load More" button visibility.
        /// </summary>
        public bool HasMorePages =>
            (_currentPage + 1) * PageSize < _filteredTools.Count;

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ShowQrCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand LoadMoreCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public ToolDetailsViewModel(FirebaseService firebase, ThemeService theme)
        {
            _firebase = firebase;
            _theme = theme;
            Title = "Tools & QR Codes";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            RefreshCommand = new Command(async () => await RefreshAsync());
            ToggleThemeCommand = new Command(() => _theme.Toggle());
            ClearFiltersCommand = new Command(ClearFilters);
            LoadMoreCommand = new Command(LoadNextPage, () => HasMorePages);

            ShowQrCommand = new Command<Tool>(async tool =>
            {
                if (tool is null) return;
                await Shell.Current.GoToAsync(
                    $"{nameof(QrDisplayView)}" +
                    $"?toolId={Uri.EscapeDataString(tool.ToolId)}" +
                    $"&toolName={Uri.EscapeDataString(tool.ToolName)}" +
                    $"&status={Uri.EscapeDataString(tool.Status)}" +
                    $"&catalogName={Uri.EscapeDataString(tool.ToolName)}");
            });
        }

        // ── Load ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Fetches tools and catalogs from Firebase (parallel).
        /// Does NOT render any tools — waits for the user to pick a catalog.
        /// forceRefresh = true only on pull-to-refresh.
        /// </summary>
        public async Task LoadAsync(bool forceRefresh = false)
        {
            IsBusy = true;
            try
            {
                var toolsTask = _firebase.GetAllToolsAsync(forceRefresh);
                var catalogsTask = _firebase.GetAllCatalogsAsync(forceRefresh);
                await Task.WhenAll(toolsTask, catalogsTask);

                _allTools = toolsTask.Result ?? new List<Tool>();
                _allCatalogs = catalogsTask.Result ?? new List<EquipmentCatalog>();

                // Stats reflect the full unfiltered set
                TotalTools = _allTools.Count;
                AvailableTools = _allTools.Count(t => t.Status == "Available");
                BorrowedTools = _allTools.Count(t => t.Status == "Borrowed");

                // ── Catalog picker ────────────────────────────────────────────
                bool isFirstLoad = Catalogs.Count == 0;

                if (isFirstLoad || forceRefresh)
                {
                    var previouslySelectedId = _selectedCatalog?.CatalogId;

                    Catalogs.Clear();

                    // "Select a catalog" placeholder — CatalogId = null signals
                    // that no real catalog is chosen yet (NoCatalogSelected = true).
                    Catalogs.Add(new EquipmentCatalog
                    {
                        CatalogId = null,           // <-- null sentinel, not empty string
                        CatalogName = "Select a catalog…"
                    });

                    foreach (var c in _allCatalogs.OrderBy(c => c.CatalogName))
                        Catalogs.Add(c);

                    if (isFirstLoad)
                    {
                        // Start with the placeholder selected → no tools rendered
                        _selectedCatalog = Catalogs[0];
                    }
                    else
                    {
                        // Pull-to-refresh: restore previous selection if still valid
                        _selectedCatalog = Catalogs
                            .FirstOrDefault(c => c.CatalogId == previouslySelectedId)
                            ?? Catalogs[0];
                    }

                    OnPropertyChanged(nameof(SelectedCatalog));
                    OnPropertyChanged(nameof(NoCatalogSelected));
                }

                // Only apply filters (and render tools) if a real catalog is chosen.
                // If the placeholder is still active, just clear the list.
                if (NoCatalogSelected)
                {
                    _filteredTools = new List<Tool>();
                    Tools = new ObservableCollection<Tool>();
                    _currentPage = 0;
                }
                else
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ToolDetailsVM] Load error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                NotifyPageStates();
            }
        }

        // Pull-to-refresh: force Firebase + reset pagination
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            _currentPage = 0;
            await LoadAsync(forceRefresh: true);
            IsRefreshing = false;
        }

        // ── Filter + pagination logic ─────────────────────────────────────────

        /// <summary>
        /// Runs the full filter pass and resets to page 1.
        /// Called whenever SearchText, SelectedCatalog, or SelectedStatus changes.
        /// </summary>
        private void ApplyFilters()
        {
            // Nothing to show until a real catalog is selected
            if (NoCatalogSelected)
            {
                _filteredTools = new List<Tool>();
                Tools = new ObservableCollection<Tool>();
                _currentPage = 0;
                NotifyPageStates();
                return;
            }

            var filtered = _allTools.AsEnumerable();

            // Catalog filter (always applied when a real catalog is selected)
            filtered = filtered.Where(t =>
                t.CatalogId == _selectedCatalog!.CatalogId);

            // Search: match tool ID or tool name (case-insensitive)
            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(t =>
                    t.ToolId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.ToolName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            // Status filter
            if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "All Status")
                filtered = filtered.Where(t =>
                    t.Status.Equals(SelectedStatus, StringComparison.OrdinalIgnoreCase));

            _filteredTools = filtered.OrderBy(t => t.ToolId).ToList();
            _currentPage = 0;

            // Show only the first page
            var firstPage = _filteredTools.Take(PageSize);
            Tools = new ObservableCollection<Tool>(firstPage);

            NotifyPageStates();
        }

        /// <summary>
        /// Appends the next page of results to the existing Tools collection.
        /// Triggered by the "Load More" button in the CollectionView footer.
        /// Appending (not replacing) keeps the scroll position intact.
        /// </summary>
        private void LoadNextPage()
        {
            if (!HasMorePages) return;

            _currentPage++;

            var nextPage = _filteredTools
                .Skip(_currentPage * PageSize)
                .Take(PageSize);

            foreach (var tool in nextPage)
                Tools.Add(tool);

            NotifyPageStates();
        }

        private void ClearFilters()
        {
            _searchText = string.Empty;
            _selectedStatus = string.Empty;

            // Reset to placeholder — forces user to re-select
            _selectedCatalog = Catalogs.FirstOrDefault();

            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(SelectedStatus));
            OnPropertyChanged(nameof(SelectedCatalog));
            OnPropertyChanged(nameof(NoCatalogSelected));

            ApplyFilters();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void NotifyPageStates()
        {
            OnPropertyChanged(nameof(NoTools));
            OnPropertyChanged(nameof(HasMorePages));
            OnPropertyChanged(nameof(NoCatalogSelected));
            ((Command)LoadMoreCommand).ChangeCanExecute();
        }
    }
}