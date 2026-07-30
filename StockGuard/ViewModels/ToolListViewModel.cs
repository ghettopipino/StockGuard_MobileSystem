using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    [QueryProperty(nameof(CatalogId), "catalogId")]
    [QueryProperty(nameof(CatalogName), "catalogName")]
    public class ToolListViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly ThemeService _theme;

        // ── Query Properties ──────────────────────────────────────────────────
        private string _catalogId = string.Empty;
        public string CatalogId
        {
            get => _catalogId;
            set => SetProperty(ref _catalogId, value);
            // ✔ Property is set. That's all.
            // ✗ DO NOT call LoadToolsAsync() here.
            //
            // Why: Shell sets CatalogId (and CatalogName) synchronously during
            // navigation, BEFORE OnAppearing fires. Queuing an async load here
            // via MainThread.BeginInvokeOnMainThread races against the Shell
            // navigation completion and can freeze ".." back navigation because
            // the navigation stack isn't fully committed yet.
            //
            // OnAppearing in ToolListView.xaml.cs is the correct single place
            // to trigger LoadToolsAsync() — Shell guarantees QueryProperty
            // setters run before OnAppearing.
        }

        private string _catalogName = string.Empty;
        public string CatalogName
        {
            get => _catalogName;
            set => SetProperty(ref _catalogName, value);
        }

        // ── Stats ─────────────────────────────────────────────────────────────
        private int _availableCount;
        public int AvailableCount
        {
            get => _availableCount;
            private set => SetProperty(ref _availableCount, value);
        }

        private int _borrowedCount;
        public int BorrowedCount
        {
            get => _borrowedCount;
            private set => SetProperty(ref _borrowedCount, value);
        }

        private int _damagedCount;
        public int DamagedCount
        {
            get => _damagedCount;
            private set => SetProperty(ref _damagedCount, value);
        }

        public string ToolCountLabel => $"{Tools.Count} tools in catalog";

        // ── Collections ───────────────────────────────────────────────────────
        public ObservableCollection<Tool> Tools { get; } = new();

        // ── Pull to Refresh ───────────────────────────────────────────────────
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ShowQrCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public ToolListViewModel(FirebaseService firebase, ThemeService theme)
        {
            _firebase = firebase;
            _theme = theme;

            // ✔ ".." = pop one level in Shell stack.
            // ✔ Shell.Current is always non-null inside a Shell app.
            // ✗ Never use Navigation.PopAsync() — it bypasses Shell.
            GoBackCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            RefreshCommand = new Command(async () => await RefreshAsync());

            ShowQrCommand = new Command<Tool>(async tool => await ShowQrAsync(tool));
        }

        // ── Load ──────────────────────────────────────────────────────────────
        public async Task LoadToolsAsync()
        {
            if (string.IsNullOrEmpty(CatalogId)) return;

            IsBusy = true;
            try
            {
                var tools = await _firebase.GetToolsByCatalogAsync(CatalogId);

                Tools.Clear();
                foreach (var tool in tools.OrderBy(t => t.ToolId))
                    Tools.Add(tool);

                AvailableCount = tools.Count(t => t.Status == "Available");
                BorrowedCount = tools.Count(t => t.Status == "Borrowed");
                DamagedCount = tools.Count(t => t.Status is "Damaged" or "UnderRepair");

                OnPropertyChanged(nameof(ToolCountLabel));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTools error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadToolsAsync();
            IsRefreshing = false;
        }

        // ── QR ────────────────────────────────────────────────────────────────
        private async Task ShowQrAsync(Tool tool)
        {
            if (tool is null) return;

            await Shell.Current.GoToAsync(
                $"{nameof(QrDisplayView)}" +
                $"?toolId={Uri.EscapeDataString(tool.ToolId)}" +
                $"&toolName={Uri.EscapeDataString(tool.ToolName)}" +
                $"&status={Uri.EscapeDataString(tool.Status)}" +
                $"&catalogName={Uri.EscapeDataString(CatalogName)}");
        }
    }
}