using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    // ─────────────────────────────────────────────────────
    // PRINT SELECTION ITEM
    // ─────────────────────────────────────────────────────
    //
    // UI-only wrapper for equipment QR selection.
    //
    // This does NOT change the Tool model and is NOT
    // saved to Firebase.
    // ─────────────────────────────────────────────────────

    public class ToolPrintItem : BaseViewModel
    {
        public Tool Tool { get; }


        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(
                        ref _isSelected,
                        value))
                {
                    OnPropertyChanged(
                        nameof(SelectionText));

                    OnPropertyChanged(
                        nameof(SelectionIcon));

                    OnPropertyChanged(
                        nameof(SelectionBorderColor));

                    OnPropertyChanged(
                        nameof(SelectionBackgroundColor));

                    OnPropertyChanged(
                        nameof(SelectionTextColor));
                }
            }
        }


        // ─────────────────────────────────────────────────
        // TOOL PROPERTIES
        // ─────────────────────────────────────────────────

        public string ToolId =>
            Tool.ToolId;


        public string ToolName =>
            Tool.ToolName;


        public string Status =>
            Tool.Status;


        public string Condition =>
            Tool.Condition;


        public string AssignedWorkerName =>
            Tool.AssignedWorkerName;


        public string BorrowedProjectName =>
            Tool.BorrowedProjectName;


        public bool IsBorrowed =>
            Tool.IsBorrowed;


        public string StatusColor =>
            Tool.StatusColor;


        // ─────────────────────────────────────────────────
        // SELECTION VISUALS
        // ─────────────────────────────────────────────────

        public string SelectionText =>
            IsSelected
                ? "Selected"
                : "Select";


        public string SelectionIcon =>
            IsSelected
                ? "\uf058"
                : "\uf111";


        public Color SelectionBorderColor =>
            IsSelected
                ? GetResourceColor(
                    "Blue",
                    Colors.DodgerBlue)
                : GetResourceColor(
                    "BorderColor",
                    Colors.LightGray);


        public Color SelectionBackgroundColor =>
            IsSelected
                ? GetResourceColor(
                    "BgElevated",
                    Colors.White)
                : GetResourceColor(
                    "BgCard",
                    Colors.White);


        public Color SelectionTextColor =>
            IsSelected
                ? GetResourceColor(
                    "Blue",
                    Colors.DodgerBlue)
                : GetResourceColor(
                    "Text3",
                    Colors.Gray);


        public ToolPrintItem(
            Tool tool)
        {
            Tool = tool;
        }


        private static Color GetResourceColor(
            string key,
            Color fallback)
        {
            if (Application.Current?.Resources
                    .TryGetValue(
                        key,
                        out var value) == true
                &&
                value is Color color)
            {
                return color;
            }


            return fallback;
        }
    }



    [QueryProperty(nameof(CatalogId), "catalogId")]
    [QueryProperty(nameof(CatalogName), "catalogName")]
    public class ToolListViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly ThemeService _theme;
        private readonly QrPrintService _qrPrintService;


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
        // PHYSICAL INVENTORY COUNTS
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


        public ObservableCollection<ToolPrintItem> PrintItems { get; }
            = new();


        // ─────────────────────────────────────────────────────
        // QR PRINT SELECTION
        // ─────────────────────────────────────────────────────

        private bool _isPrintSelectionMode;

        public bool IsPrintSelectionMode
        {
            get => _isPrintSelectionMode;
            private set
            {
                SetProperty(
                    ref _isPrintSelectionMode,
                    value);

                OnPropertyChanged(
                    nameof(IsNormalMode));
            }
        }


        public bool IsNormalMode =>
            !IsPrintSelectionMode;


        private int _selectedPrintCount;

        public int SelectedPrintCount
        {
            get => _selectedPrintCount;
            private set
            {
                SetProperty(
                    ref _selectedPrintCount,
                    value);

                OnPropertyChanged(
                    nameof(SelectedPrintLabel));

                OnPropertyChanged(
                    nameof(PrintSelectedButtonText));

                OnPropertyChanged(
                    nameof(HasPrintSelection));
            }
        }


        public string SelectedPrintLabel =>
            SelectedPrintCount == 1
                ? "1 equipment selected"
                : $"{SelectedPrintCount} equipment selected";


        public string PrintSelectedButtonText =>
            $"Print Selected ({SelectedPrintCount})";


        public bool HasPrintSelection =>
            SelectedPrintCount > 0;


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

        public ICommand PrintQrCommand { get; }

        public ICommand CancelPrintSelectionCommand { get; }

        public ICommand TogglePrintToolCommand { get; }

        public ICommand SelectAllPrintCommand { get; }

        public ICommand PrintSelectedCommand { get; }


        // ─────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────

        public ToolListViewModel(
            FirebaseService firebase,
            ThemeService theme,
            QrPrintService qrPrintService)
        {
            _firebase = firebase;
            _theme = theme;
            _qrPrintService = qrPrintService;


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


            PrintQrCommand =
                new Command(
                    async () =>
                        await PrintQrLabelsAsync());


            CancelPrintSelectionCommand =
                new Command(
                    CancelPrintSelection);


            TogglePrintToolCommand =
                new Command<ToolPrintItem>(
                    TogglePrintTool);


            SelectAllPrintCommand =
                new Command(
                    SelectAllForPrint);


            PrintSelectedCommand =
                new Command(
                    async () =>
                        await PrintSelectedAsync(),
                    () =>
                        HasPrintSelection);
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
                // BUILD PRINT ITEMS
                // ─────────────────────────────────────────────

                PrintItems.Clear();


                foreach (var tool in Tools)
                {
                    PrintItems.Add(
                        new ToolPrintItem(
                            tool));
                }


                UpdatePrintSelectionCount();


                // ─────────────────────────────────────────────
                // AVAILABLE
                // ─────────────────────────────────────────────
                //
                // Physical tools currently available
                // in the office.
                // ─────────────────────────────────────────────

                AvailableCount =
                    tools.Count(t =>
                        !t.IsDeleted &&
                        string.Equals(
                            t.Status,
                            "Available",
                            StringComparison.OrdinalIgnoreCase));


                // ─────────────────────────────────────────────
                // BORROWED
                // ─────────────────────────────────────────────
                //
                // Borrowed includes:
                //
                // 1. Tool borrowed from office by PE
                //    but not yet distributed to a worker.
                //
                // 2. Tool accepted by a worker.
                //
                // 3. PendingReturn because the physical tool
                //    has not yet been approved as returned.
                //
                // Accountability can therefore be:
                //
                // PE     → AssignedWorkerId is empty
                // Worker → AssignedWorkerId has a value
                //
                // Inventory Status remains Borrowed.
                // ─────────────────────────────────────────────

                BorrowedCount =
                    tools.Count(t =>
                        !t.IsDeleted &&
                        (
                            string.Equals(
                                t.Status,
                                "Borrowed",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            string.Equals(
                                t.Status,
                                "PendingReturn",
                                StringComparison.OrdinalIgnoreCase)
                        ));


                // ─────────────────────────────────────────────
                // DAMAGED
                // ─────────────────────────────────────────────

                DamagedCount =
                    tools.Count(t =>
                        !t.IsDeleted &&
                        (
                            string.Equals(
                                t.Status,
                                "Damaged",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            string.Equals(
                                t.Status,
                                "UnderRepair",
                                StringComparison.OrdinalIgnoreCase)
                        ));


                // ─────────────────────────────────────────────
                // LOST
                // ─────────────────────────────────────────────

                LostCount =
                    tools.Count(t =>
                        !t.IsDeleted &&
                        string.Equals(
                            t.Status,
                            "Lost",
                            StringComparison.OrdinalIgnoreCase));


                // ─────────────────────────────────────────────
                // UPDATE LABEL
                // ─────────────────────────────────────────────

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


        // ─────────────────────────────────────────────────────
        // PRINT QR LABELS
        // ─────────────────────────────────────────────────────

        private async Task PrintQrLabelsAsync()
        {
            if (Tools.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Equipment",
                    "There are no equipment QR labels to print.",
                    "OK");

                return;
            }


            var printAllText =
                $"Print All ({Tools.Count})";


            var action =
                await Shell.Current.DisplayActionSheet(
                    "Print QR Labels",
                    "Cancel",
                    null,
                    printAllText,
                    "Select Equipment");


            // ─────────────────────────────────────────────
            // PRINT ALL
            // ─────────────────────────────────────────────

            if (action == printAllText)
            {
                await _qrPrintService.PrintLabelsAsync(
                    Tools.ToList(),
                    CatalogName);

                return;
            }


            // ─────────────────────────────────────────────
            // SELECT EQUIPMENT
            // ─────────────────────────────────────────────

            if (action == "Select Equipment")
            {
                StartPrintSelection();
            }
        }


        // ─────────────────────────────────────────────────────
        // START PRINT SELECTION
        // ─────────────────────────────────────────────────────

        private void StartPrintSelection()
        {
            foreach (var item in PrintItems)
            {
                item.IsSelected = false;
            }


            IsPrintSelectionMode = true;

            UpdatePrintSelectionCount();
        }


        // ─────────────────────────────────────────────────────
        // CANCEL PRINT SELECTION
        // ─────────────────────────────────────────────────────

        private void CancelPrintSelection()
        {
            foreach (var item in PrintItems)
            {
                item.IsSelected = false;
            }


            IsPrintSelectionMode = false;

            UpdatePrintSelectionCount();
        }


        // ─────────────────────────────────────────────────────
        // TOGGLE TOOL FOR PRINTING
        // ─────────────────────────────────────────────────────

        private void TogglePrintTool(
            ToolPrintItem item)
        {
            if (item == null)
                return;


            item.IsSelected =
                !item.IsSelected;


            UpdatePrintSelectionCount();
        }


        // ─────────────────────────────────────────────────────
        // SELECT ALL FOR PRINTING
        // ─────────────────────────────────────────────────────

        private void SelectAllForPrint()
        {
            var shouldSelectAll =
                PrintItems.Any(
                    item =>
                        !item.IsSelected);


            foreach (var item in PrintItems)
            {
                item.IsSelected =
                    shouldSelectAll;
            }


            UpdatePrintSelectionCount();
        }


        // ─────────────────────────────────────────────────────
        // UPDATE PRINT SELECTION COUNT
        // ─────────────────────────────────────────────────────

        private void UpdatePrintSelectionCount()
        {
            SelectedPrintCount =
                PrintItems.Count(
                    item =>
                        item.IsSelected);


            if (PrintSelectedCommand is Command command)
            {
                command.ChangeCanExecute();
            }
        }


        // ─────────────────────────────────────────────────────
        // PRINT SELECTED
        // ─────────────────────────────────────────────────────

        private async Task PrintSelectedAsync()
        {
            var selectedTools =
                PrintItems
                    .Where(item =>
                        item.IsSelected)
                    .Select(item =>
                        item.Tool)
                    .OrderBy(tool =>
                        tool.ToolId)
                    .ToList();


            if (selectedTools.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Selection",
                    "Select at least one equipment item.",
                    "OK");

                return;
            }


            await _qrPrintService.PrintLabelsAsync(
                selectedTools,
                CatalogName);


            CancelPrintSelection();
        }
    }
}