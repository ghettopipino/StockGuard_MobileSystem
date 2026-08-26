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

        // ─────────────────────────────────────────────────────
        // QUERY PROPERTIES
        // ─────────────────────────────────────────────────────

        private string _catalogId = string.Empty;

        public string CatalogId
        {
            get => _catalogId;
            set => SetProperty(
                ref _catalogId,
                value);
        }

        private string _catalogName = string.Empty;

        public string CatalogName
        {
            get => _catalogName;
            set => SetProperty(
                ref _catalogName,
                value);
        }


        // ─────────────────────────────────────────────────────
        // PHYSICAL INVENTORY
        // ─────────────────────────────────────────────────────

        private int _availableCount;

        public int AvailableCount
        {
            get => _availableCount;
            private set => SetProperty(
                ref _availableCount,
                value);
        }


        private int _borrowedCount;

        public int BorrowedCount
        {
            get => _borrowedCount;
            private set => SetProperty(
                ref _borrowedCount,
                value);
        }


        private int _damagedCount;

        public int DamagedCount
        {
            get => _damagedCount;
            private set => SetProperty(
                ref _damagedCount,
                value);
        }


        private int _lostCount;

        public int LostCount
        {
            get => _lostCount;
            private set => SetProperty(
                ref _lostCount,
                value);
        }


        // ─────────────────────────────────────────────────────
        // PROJECT ALLOCATION
        // ─────────────────────────────────────────────────────

        private int _allocatedCount;

        public int AllocatedCount
        {
            get => _allocatedCount;
            private set => SetProperty(
                ref _allocatedCount,
                value);
        }


        // ─────────────────────────────────────────────────────
        // LABEL
        // ─────────────────────────────────────────────────────

        public string ToolCountLabel =>
            Tools.Count == 1
                ? "1 tool in catalog"
                : $"{Tools.Count} tools in catalog";


        // ─────────────────────────────────────────────────────
        // COLLECTION
        // ─────────────────────────────────────────────────────

        public ObservableCollection<Tool> Tools { get; }
            = new();


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
        // COMMANDS
        // ─────────────────────────────────────────────────────

        public ICommand GoBackCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand ShowQrCommand { get; }


        // ─────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────

        public ToolListViewModel(
            FirebaseService firebase,
            ThemeService theme)
        {
            _firebase = firebase;
            _theme = theme;

            GoBackCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(".."));

            RefreshCommand =
                new Command(
                    async () =>
                        await RefreshAsync());

            ShowQrCommand =
                new Command<Tool>(
                    async tool =>
                        await ShowQrAsync(tool));
        }


        // ─────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────

        public async Task LoadToolsAsync()
        {
            if (string.IsNullOrWhiteSpace(
                    CatalogId))
            {
                return;
            }

            IsBusy = true;

            try
            {
                // ─────────────────────────────────────────────
                // LOAD PHYSICAL TOOLS
                // ─────────────────────────────────────────────

                var tools =
                    await _firebase
                        .GetToolsByCatalogAsync(
                            CatalogId);


                // ─────────────────────────────────────────────
                // LOAD ACTIVE PROJECT ALLOCATIONS
                // ─────────────────────────────────────────────

                var allocations =
                    await _firebase
                        .GetAllActiveProjectEquipmentRequirementsAsync();


                // ─────────────────────────────────────────────
                // DISPLAY TOOLS
                // ─────────────────────────────────────────────

                Tools.Clear();

                foreach (var tool in
                         tools
                             .Where(t =>
                                 !t.IsDeleted)
                             .OrderBy(t =>
                                 t.ToolId))
                {
                    Tools.Add(tool);
                }


                // ─────────────────────────────────────────────
                // PHYSICAL AVAILABLE
                // ─────────────────────────────────────────────
                //
                // This is ACTUAL physical availability.
                //
                // Project allocation does NOT reduce this
                // number unless the physical tool status
                // itself changes.
                // ─────────────────────────────────────────────

                AvailableCount =
                    tools.Count(t =>
                        !t.IsDeleted &&
                        t.Status ==
                            "Available");


                // ─────────────────────────────────────────────
                // ACTUAL BORROWED
                // ─────────────────────────────────────────────
                //
                // Borrowed means:
                //
                // PE distributed a specific tool
                //        ↓
                // worker accepted it
                //        ↓
                // Tool.Status = Borrowed
                //
                // PendingReturn still counts because the
                // worker physically has the tool until the
                // PE approves the return.
                // ─────────────────────────────────────────────

                BorrowedCount =
                    tools.Count(t =>
                        !t.IsDeleted &&
                        (
                            t.Status ==
                                "Borrowed" ||

                            t.Status ==
                                "PendingReturn"
                        ));


                // ─────────────────────────────────────────────
                // DAMAGED
                // ─────────────────────────────────────────────

                DamagedCount =
                    tools.Count(t =>
                        !t.IsDeleted &&
                        (
                            t.Status ==
                                "Damaged" ||

                            t.Status ==
                                "UnderRepair"
                        ));


                // ─────────────────────────────────────────────
                // LOST
                // ─────────────────────────────────────────────

                LostCount =
                    tools.Count(t =>
                        !t.IsDeleted &&
                        t.Status ==
                            "Lost");


                // ─────────────────────────────────────────────
                // PROJECT ALLOCATION
                // ─────────────────────────────────────────────
                //
                // Allocation is PE/project accountability.
                //
                // It is NOT worker borrowing.
                // ─────────────────────────────────────────────

                AllocatedCount =
                    allocations
                        .Where(a =>
                            string.Equals(
                                a.CatalogId,
                                CatalogId,
                                StringComparison
                                    .OrdinalIgnoreCase))
                        .Sum(a =>
                            a.QuantityNeeded);


                OnPropertyChanged(
                    nameof(
                        ToolCountLabel));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug
                    .WriteLine(
                        $"LoadTools error: " +
                        $"{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }


        // ─────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────

        private async Task RefreshAsync()
        {
            IsRefreshing = true;

            try
            {
                await LoadToolsAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }


        // ─────────────────────────────────────────────────────
        // QR
        // ─────────────────────────────────────────────────────

        private async Task ShowQrAsync(
            Tool tool)
        {
            if (tool == null)
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
                    $"{Uri.EscapeDataString(CatalogName)}");
        }
    }
}