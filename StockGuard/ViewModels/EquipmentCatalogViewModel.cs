using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    public class EquipmentCatalogViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;
        private IDisposable? _globalListener;
        private CancellationTokenSource? _debounceCts;
        // ── Theme ─────────────────────────────────────────────────────────────
        public string ThemeIcon => _theme.IsDark ? "🌙" : "☀️";

        // ── Stats ─────────────────────────────────────────────────────────────
        private int _totalCatalogs;
        public int TotalCatalogs
        {
            get => _totalCatalogs;
            private set => SetProperty(ref _totalCatalogs, value);
        }

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

        // ── Collections ───────────────────────────────────────────────────────
        public ObservableCollection<CatalogDisplayItem> Catalogs { get; } = new();

        // ── Empty State ───────────────────────────────────────────────────────
        private bool _hasCatalogs;
        public bool HasCatalogs
        {
            get => _hasCatalogs;
            private set
            {
                SetProperty(ref _hasCatalogs, value);
                OnPropertyChanged(nameof(NoCatalogs));
            }
        }
        public bool NoCatalogs => !HasCatalogs;

        // ── Search ────────────────────────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadCatalogsAsync();
                    StartLiveSync();
                });
            }
        }
        private void StartLiveSync()
        {
            _globalListener = _firebase.StartGlobalListenerDisposable(() =>
            {
                _firebase.InvalidateToolCache();
                _firebase.InvalidateCatalogCache();

                // Cancel previous pending reload
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
                var token = _debounceCts.Token;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Wait 800ms — if another change fires, this gets cancelled
                        await Task.Delay(800, token);
                        await LoadCatalogsAsync();
                    }
                    catch (TaskCanceledException) { /* debounced, ignore */ }
                });
            });
        }


        // ── Pull to Refresh ───────────────────────────────────────────────────
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // ── Commands ──────────────────────────────────────────────────────────
        //
        // GoBackCommand    → REMOVED. EquipmentCatalogView is a FlyoutItem (root
        //                    page). Root pages have nothing to pop — GoToAsync("..")
        //                    either throws or silently does nothing. The ← button
        //                    in the old header was misleading and broken. It is now
        //                    replaced by OpenFlyoutCommand (☰) from BaseViewModel.
        //
        // OpenFlyoutCommand → inherited from BaseViewModel. Bound to the ☰ button
        //                     in EquipmentCatalogView.xaml header.

        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand AddCatalogCommand { get; }
        public ICommand ViewToolsCommand { get; }
        public ICommand DeleteCatalogCommand { get; }
        public ICommand AddToolsCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public EquipmentCatalogViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;
            Title = "Equipment Catalog";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            RefreshCommand = new Command(async () => await RefreshAsync());
            ToggleThemeCommand = new Command(() => _theme.Toggle());
            AddCatalogCommand = new Command(async () => await AddCatalogAsync());
            ViewToolsCommand = new Command<CatalogDisplayItem>(
                                       async c => await ViewToolsAsync(c));
            DeleteCatalogCommand = new Command<CatalogDisplayItem>(
                                       async c => await DeleteCatalogAsync(c));
            AddToolsCommand = new Command<CatalogDisplayItem>(
    async c => await AddToolsAsync(c));
            MainThread.BeginInvokeOnMainThread(
                async () => await LoadCatalogsAsync());
        }

        // ── Load ──────────────────────────────────────────────────────────────
        public async Task LoadCatalogsAsync()
        {
            IsBusy = true;
            try
            {
                var catalogs = await _firebase.GetAllCatalogsAsync();
                var allTools = await _firebase.GetAllToolsAsync();

                if (!string.IsNullOrWhiteSpace(SearchText))
                    catalogs = catalogs
                        .Where(c => c.CatalogName.Contains(
                            SearchText, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                TotalCatalogs = catalogs.Count;
                TotalTools = allTools.Count;
                AvailableTools = allTools.Count(t => t.Status == "Available");

                Catalogs.Clear();
                foreach (var catalog in catalogs)
                {
                    var tools = allTools.Where(t => t.CatalogId == catalog.CatalogId).ToList();
                    var available = tools.Count(t => t.Status == "Available");
                    var borrowed = tools.Count(t => t.Status == "Borrowed");
                    var damaged = tools.Count(t => t.Status is "Damaged" or "UnderRepair");

                    Catalogs.Add(new CatalogDisplayItem(catalog)
                    {
                        TotalTools = tools.Count,
                        AvailableTools = available,
                        BorrowedTools = borrowed,
                        DamagedTools = damaged
                    });
                }

                HasCatalogs = Catalogs.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadCatalogs error: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadCatalogsAsync();
            IsRefreshing = false;
        }

        // ── Navigate to ToolListView ───────────────────────────────────────────
        private async Task ViewToolsAsync(CatalogDisplayItem item)
        {
            if (item is null) return;

            await Shell.Current.GoToAsync(
                $"{nameof(ToolListView)}" +
                $"?catalogId={Uri.EscapeDataString(item.CatalogId)}" +
                $"&catalogName={Uri.EscapeDataString(item.CatalogName)}");
        }

        // ── Add Catalog ───────────────────────────────────────────────────────
        private async Task AddCatalogAsync()
        {
            var name = await Shell.Current.DisplayPromptAsync(
                "New Equipment Catalog", "Enter the tool/equipment name:",
                "Next", "Cancel", placeholder: "e.g. Power Drill", maxLength: 50);
            if (string.IsNullOrWhiteSpace(name)) return;

            var prefix = await Shell.Current.DisplayPromptAsync(
                "Tool ID Prefix", $"Enter a short prefix for {name} IDs:",
                "Next", "Cancel", placeholder: "e.g. PD (for Power Drill)", maxLength: 6);
            if (string.IsNullOrWhiteSpace(prefix)) return;
            prefix = prefix.ToUpper().Trim();

            var qtyStr = await Shell.Current.DisplayPromptAsync(
                "Quantity", $"How many {name}s to add?",
                "Create", "Cancel", placeholder: "e.g. 5",
                keyboard: Microsoft.Maui.Keyboard.Numeric);
            if (string.IsNullOrWhiteSpace(qtyStr)) return;

            if (!int.TryParse(qtyStr, out int qty) || qty <= 0 || qty > 100)
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Quantity", "Please enter a number between 1 and 100.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var catalogId = $"CAT-{prefix}-{DateTime.Now:yyyyMMddHHmmss}";
                var catalog = new EquipmentCatalog
                {
                    CatalogId = catalogId,
                    CatalogName = name.Trim(),
                    Prefix = prefix,
                    Quantity = qty,
                    DateCreated = DateTime.Now,
                    IsDeleted = false
                };
                await _firebase.CreateCatalogAsync(catalog);

                for (int i = 1; i <= qty; i++)
                {
                    var toolId = $"{prefix}-{i.ToString().PadLeft(3, '0')}";
                    var existing = await _firebase.GetToolByIdAsync(toolId);
                    if (existing != null)
                        toolId = $"{prefix}-{DateTime.Now.Ticks % 10000}-{i.ToString().PadLeft(3, '0')}";

                    await _firebase.CreateToolAsync(new Tool
                    {
                        ToolId = toolId,
                        ToolName = name.Trim(),
                        CatalogId = catalogId,
                        Status = "Available",
                        QrCode = toolId,
                        Condition = "Good",
                        IsDeleted = false
                    });
                }

                await Shell.Current.DisplayAlert(
                    "✅ Catalog Created",
                    $"{name} catalog created with {qty} tools.\n\n" +
                    $"Tool IDs: {prefix}-001 to {prefix}-{qty.ToString().PadLeft(3, '0')}\n\n",
                    $"Print QR codes for each Tool ID and attach them to the physical tools.",
                    "OK");

                await LoadCatalogsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error", $"Could not create catalog.\n{ex.Message}", "OK");
            }
            finally { IsBusy = false; }
        }

        // ── Delete Catalog ────────────────────────────────────────────────────
        private async Task DeleteCatalogAsync(CatalogDisplayItem item)
        {
            if (item is null || IsBusy) return;

            var tools = await _firebase.GetToolsByCatalogAsync(item.CatalogId);
            var activeTool = tools.FirstOrDefault(
                t => t.Status is "Borrowed" or "Damaged");

            if (activeTool != null)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Delete",
                    $"This catalog has tools that are currently {activeTool.Status}.\n\n" +
                    $"Please ensure all tools are available before deleting this catalog.",
                    "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Catalog",
                $"Delete {item.CatalogName} and all {tools.Count} tools?\n\nThis cannot be undone.",
                "Delete", "Cancel");
            if (!confirm) return;

            IsBusy = true;
            try
            {
                foreach (var tool in tools)
                {
                    tool.IsDeleted = true;
                    await _firebase.UpdateToolAsync(tool);
                }
                await _firebase.DeleteCatalogAsync(item.CatalogId);

                await Shell.Current.DisplayAlert(
                    "✅ Deleted",
                    $"{item.CatalogName} catalog and all its tools have been removed.",
                    "OK");

                await LoadCatalogsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error", $"Could not delete catalog.\n{ex.Message}", "OK");
            }
            finally { IsBusy = false; }
        }
        // ── Add Tools to Existing Catalog ─────────────────────────────────
        private async Task AddToolsAsync(CatalogDisplayItem item)
        {
            if (item is null || IsBusy) return;

            // Ask how many tools to add
            var qtyStr = await Shell.Current.DisplayPromptAsync(
                $"Add Tools — {item.CatalogName}",
                $"How many new tools do you want to add?\n" +
                $"Current count: {item.TotalTools}  |  " +
                $"Prefix: {item.Prefix}",
                "Next", "Cancel",
                placeholder: "e.g. 3",
                keyboard: Microsoft.Maui.Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyStr)) return;

            if (!int.TryParse(qtyStr, out int qty) || qty <= 0 || qty > 100)
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Quantity",
                    "Please enter a number between 1 and 100.",
                    "OK");
                return;
            }

            // Ask for tool name (defaults to catalog name)
            var toolName = await Shell.Current.DisplayPromptAsync(
                "Tool Name",
                $"Name for the new tool(s).\n" +
                $"Leave blank to use catalog name.",
                "Add", "Cancel",
                placeholder: item.CatalogName,
                initialValue: item.CatalogName,
                maxLength: 50);

            if (toolName is null) return; // user hit Cancel
            if (string.IsNullOrWhiteSpace(toolName))
                toolName = item.CatalogName;
            toolName = toolName.Trim();

            IsBusy = true;
            try
            {
                // Find highest existing number for this catalog's prefix
                var allTools = await _firebase.GetAllToolsAsync(forceRefresh: true);
                var existingNumbers = allTools
                    .Where(t => t.CatalogId == item.CatalogId)
                    .Select(t =>
                    {
                        var parts = t.ToolId.Split('-');
                        return parts.Length >= 2 &&
                               int.TryParse(parts.Last(), out int n) ? n : 0;
                    })
                    .ToList();

                int nextNumber = existingNumbers.Count > 0
                    ? existingNumbers.Max() + 1
                    : item.TotalTools + 1;

                int added = 0;
                var firstId = string.Empty;
                var lastId = string.Empty;

                for (int i = 0; i < qty; i++)
                {
                    var toolId =
                        $"{item.Prefix}" +
                        $"-{nextNumber.ToString().PadLeft(3, '0')}";

                    // Guarantee uniqueness
                    var existing = await _firebase.GetToolByIdAsync(toolId);
                    if (existing != null)
                        toolId =
                            $"{item.Prefix}" +
                            $"-{DateTime.Now.Ticks % 10000}" +
                            $"-{nextNumber.ToString().PadLeft(3, '0')}";

                    if (added == 0) firstId = toolId;
                    lastId = toolId;
                    nextNumber++;

                    await _firebase.CreateToolAsync(new Tool
                    {
                        ToolId = toolId,
                        ToolName = toolName,
                        CatalogId = item.CatalogId,
                        Status = "Available",
                        QrCode = toolId,
                        Condition = "Good",
                        IsDeleted = false
                    });

                    added++;
                }

                // Update catalog quantity
                var catalogs = await _firebase.GetAllCatalogsAsync(forceRefresh: true);
                var catalog = catalogs.FirstOrDefault(
                    c => c.CatalogId == item.CatalogId);

                if (catalog != null)
                {
                    catalog.Quantity += added;
                    await _firebase.UpdateCatalogAsync(catalog);
                }

                await Shell.Current.DisplayAlert(
                    "✅ Tools Added",
                    $"{added} tool(s) added to {item.CatalogName}.\n\n" +
                    $"New Tool IDs: {firstId} to {lastId}\n\n",
                    $"Print QR codes for the new tools and attach them " +
                    $"to the physical tools.",
                    "OK");

                await LoadCatalogsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not add tools.\n{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }
    }
}