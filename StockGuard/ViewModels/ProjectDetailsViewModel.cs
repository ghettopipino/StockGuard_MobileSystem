using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    [QueryProperty(nameof(ProjectId), "projectId")]
    public class ProjectDetailsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        private bool _isLoading;

        // ─────────────────────────────────────────────────────────
        // QUERY PROPERTY
        // ─────────────────────────────────────────────────────────

        private string _projectId = string.Empty;

        public string ProjectId
        {
            get => _projectId;
            set
            {
                SetProperty(ref _projectId, value);

                if (!string.IsNullOrEmpty(value))
                {
                    MainThread.BeginInvokeOnMainThread(
                        async () => await LoadAsync());
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // PROJECT
        // ─────────────────────────────────────────────────────────

        private Project? _project;

        public Project? Project
        {
            get => _project;
            set
            {
                SetProperty(ref _project, value);

                OnPropertyChanged(nameof(ProjectName));
                OnPropertyChanged(nameof(Location));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StartDateLabel));
                OnPropertyChanged(nameof(DurationLabel));
                OnPropertyChanged(nameof(IsActive));
            }
        }

        public string ProjectName =>
            Project?.ProjectName ??
            "Loading...";

        public string Location =>
            Project?.Location ??
            string.Empty;

        public string Status =>
            Project?.Status ??
            string.Empty;

        public string StatusIcon =>
            Project?.StatusIcon ??
            "❓";

        public string StatusColor =>
            Project?.StatusColor ??
            "#94a3b8";

        public string StartDateLabel =>
            Project?.StartDateLabel ??
            string.Empty;

        public string DurationLabel =>
            Project?.DurationLabel ??
            string.Empty;

        public bool IsActive =>
            Project?.IsActive ?? false;

        // ─────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────

        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        // ─────────────────────────────────────────────────────────
        // WORKERS
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<User>
            AssignedWorkers
        { get; } = new();

        // ─────────────────────────────────────────────────────────
        // EQUIPMENT SUMMARY
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<CatalogStockSummary>
            EquipmentSummary
        { get; } = new();

        // ─────────────────────────────────────────────────────────
        // STATS
        // ─────────────────────────────────────────────────────────

        private int _workerCount;

        public int WorkerCount
        {
            get => _workerCount;
            private set =>
                SetProperty(
                    ref _workerCount,
                    value);
        }

        private int _toolCount;

        public int ToolCount
        {
            get => _toolCount;
            private set =>
                SetProperty(
                    ref _toolCount,
                    value);
        }

        private int _borrowedCount;

        public int BorrowedCount
        {
            get => _borrowedCount;
            private set =>
                SetProperty(
                    ref _borrowedCount,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // EMPTY STATES
        // ─────────────────────────────────────────────────────────

        private bool _hasWorkers;

        public bool HasWorkers
        {
            get => _hasWorkers;
            private set
            {
                SetProperty(ref _hasWorkers, value);
                OnPropertyChanged(nameof(NoWorkers));
            }
        }

        public bool NoWorkers =>
            !HasWorkers;

        private bool _hasEquipment;

        public bool HasEquipment
        {
            get => _hasEquipment;
            private set
            {
                SetProperty(ref _hasEquipment, value);
                OnPropertyChanged(nameof(NoEquipment));
            }
        }

        public bool NoEquipment =>
            !HasEquipment;

        // ─────────────────────────────────────────────────────────
        // COMMANDS
        // ─────────────────────────────────────────────────────────

        public ICommand GoBackCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand RefreshCommand { get; }

        public ICommand AssignWorkerCommand { get; }
        public ICommand RemoveWorkerCommand { get; }

        public ICommand AssignEquipmentCommand { get; }
        public ICommand AddEquipmentCommand { get; }
        public ICommand RemoveEquipmentCommand { get; }
        public ICommand DistributeCommand { get; }

        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public ProjectDetailsViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;

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

            ToggleThemeCommand =
                new Command(
                    () => _theme.Toggle());

            RefreshCommand =
                new Command(
                    async () =>
                        await LoadAsync());

            AssignEquipmentCommand =
                new Command(
                    async () =>
                        await AssignEquipmentAsync());

            AddEquipmentCommand =
                new Command(
                    async () =>
                        await AddEquipmentAsync());

            RemoveEquipmentCommand =
                new Command<CatalogStockSummary>(
                    async item =>
                        await RemoveEquipmentAsync(item));

            DistributeCommand =
                new Command<CatalogStockSummary>(
                    async item =>
                        await DistributeAsync(item));

            AssignWorkerCommand =
                new Command(
                    async () =>
                        await Shell.Current.GoToAsync(
                            $"{nameof(BulkSelectView)}" +
                            $"?projectId={ProjectId}" +
                            $"&selectMode=workers"));

            RemoveWorkerCommand =
                new Command<User>(
                    async worker =>
                        await RemoveWorkerAsync(worker));
        }

        // ─────────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────────

        public async Task LoadAsync()
        {
            if (string.IsNullOrEmpty(ProjectId))
                return;

            if (_isLoading)
                return;

            _isLoading = true;
            IsBusy = true;

            try
            {
                var projects =
                    await _firebase.GetAllProjectsAsync();

                Project =
                    projects.FirstOrDefault(p =>
                        p.ProjectId == ProjectId);

                if (Project == null)
                    return;

                // ── WORKERS ────────────────────────────────

                AssignedWorkers.Clear();

                var workerKeys =
                    await _firebase
                        .GetProjectWorkerKeysAsync(
                            ProjectId);

                var allUsers =
                    await _firebase
                        .GetAllUsersAsync();

                foreach (var key in workerKeys)
                {
                    var worker =
                        allUsers.FirstOrDefault(u =>
                            u.UniqueKey == key);

                    if (worker != null)
                    {
                        AssignedWorkers.Add(worker);
                    }
                }

                HasWorkers =
                    AssignedWorkers.Count > 0;

                WorkerCount =
                    AssignedWorkers.Count;

                // ── EQUIPMENT ──────────────────────────────

                var allTools =
                    await _firebase
                        .GetAllToolsAsync(
                            forceRefresh: true);

                await LoadEquipmentSummaryAsync(
                    allTools);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadProject error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                _isLoading = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // REMOVE WORKER
        // ─────────────────────────────────────────────────────────

        private async Task RemoveWorkerAsync(
            User worker)
        {
            if (worker == null)
                return;

            var allTools =
                await _firebase.GetAllToolsAsync(
                    forceRefresh: true);

            var workerTools =
                allTools
                    .Where(t =>
                        t.AssignedWorkerId ==
                            worker.UniqueKey &&
                        (
                            t.Status == "Borrowed" ||
                            t.Status == "PendingReturn"
                        ))
                    .ToList();

            if (workerTools.Count > 0)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Remove Worker",
                    $"{worker.FullName} still has " +
                    $"{workerTools.Count} equipment item(s) " +
                    $"under their responsibility.\n\n" +
                    "Return all equipment first.",
                    "OK");

                return;
            }

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Remove Worker",
                    $"Remove {worker.FullName} from " +
                    $"{Project?.ProjectName}?",
                    "Remove",
                    "Cancel");

            if (!confirm)
                return;

            try
            {
                await _firebase
                    .RemoveWorkerFromProjectAsync(
                        ProjectId,
                        worker.UniqueKey);

                await Shell.Current.DisplayAlert(
                    "Worker Removed",
                    $"{worker.FullName} removed from project.",
                    "OK");

                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not remove worker.\n" +
                    $"{ex.Message}",
                    "OK");
            }
        }

        // ─────────────────────────────────────────────────────────
        // ASSIGN EQUIPMENT VIA QR
        // ─────────────────────────────────────────────────────────

        private async Task AssignEquipmentAsync()
        {
            await Shell.Current.GoToAsync(
                $"{nameof(QrScannerView)}" +
                $"?mode=AssignEquipment" +
                $"&projectId={ProjectId}");
        }

        // ─────────────────────────────────────────────────────────
        // EQUIPMENT SUMMARY
        // ─────────────────────────────────────────────────────────

        private async Task LoadEquipmentSummaryAsync(
            List<Tool> allTools)
        {
            var requirements =
                await _firebase
                    .GetProjectEquipmentRequirementsAsync(
                        ProjectId);

            EquipmentSummary.Clear();

            foreach (var req in requirements)
            {
                int borrowedCount =
                    allTools.Count(t =>
                        t.CatalogId ==
                            req.CatalogId &&
                        t.BorrowedProjectId ==
                            ProjectId &&
                        (
                            t.Status == "Borrowed" ||
                            t.Status == "PendingReturn"
                        ));

                EquipmentSummary.Add(
                    new CatalogStockSummary
                    {
                        CatalogId =
                            req.CatalogId,

                        CatalogName =
                            req.CatalogName,

                        QuantityNeeded =
                            req.QuantityNeeded,

                        BorrowedCount =
                            borrowedCount
                    });
            }

            ToolCount =
                requirements.Sum(r =>
                    r.QuantityNeeded);

            BorrowedCount =
                EquipmentSummary.Sum(e =>
                    e.BorrowedCount);

            HasEquipment =
                EquipmentSummary.Count > 0;
        }

        // ─────────────────────────────────────────────────────────
        // ADD EQUIPMENT ALLOCATION
        // ─────────────────────────────────────────────────────────

        private async Task AddEquipmentAsync()
        {
            var catalogs =
                await _firebase
                    .GetAllCatalogsAsync();

            if (catalogs.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Catalog Items",
                    "Create equipment catalog entries first.",
                    "OK");

                return;
            }

            var search =
                await Shell.Current.DisplayPromptAsync(
                    "Find Equipment",
                    "Search for the equipment you want to add:",
                    "Search",
                    "Cancel",
                    placeholder:
                        "e.g. Power Drill");

            if (search == null)
                return;

            search =
                search.Trim();

            var matches =
                string.IsNullOrWhiteSpace(search)
                    ? catalogs
                    : catalogs
                        .Where(c =>
                            c.CatalogName.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();

            if (matches.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Match Found",
                    $"No equipment matched \"{search}\".",
                    "OK");

                return;
            }

            EquipmentCatalog? catalog = null;

            if (matches.Count == 1)
            {
                catalog = matches[0];
            }
            else
            {
                var names =
                    matches
                        .Select(c => c.CatalogName)
                        .ToArray();

                var selected =
                    await Shell.Current.DisplayActionSheet(
                        "Select Equipment",
                        "Cancel",
                        null,
                        names);

                if (string.IsNullOrWhiteSpace(selected) ||
                    selected == "Cancel")
                {
                    return;
                }

                catalog =
                    matches.FirstOrDefault(c =>
                        c.CatalogName == selected);
            }

            if (catalog == null)
                return;

            var allTools =
                await _firebase
                    .GetAllToolsAsync(
                        forceRefresh: true);

            var allocations =
                await _firebase
                    .GetAllActiveProjectEquipmentRequirementsAsync();

            int totalUsableTools =
                allTools.Count(t =>
                    t.CatalogId ==
                        catalog.CatalogId &&
                    t.Status != "Damaged" &&
                    t.Status != "UnderRepair" &&
                    t.Status != "Lost");

            int totalAllocated =
                allocations
                    .Where(a =>
                        a.CatalogId ==
                        catalog.CatalogId)
                    .Sum(a =>
                        a.QuantityNeeded);

            int availableNow =
                Math.Max(
                    0,
                    totalUsableTools -
                    totalAllocated);

            if (availableNow <= 0)
            {
                await Shell.Current.DisplayAlert(
                    "None Available",
                    $"All usable {catalog.CatalogName} units " +
                    "are already allocated to projects.",
                    "OK");

                return;
            }

            var requirements =
                await _firebase
                    .GetProjectEquipmentRequirementsAsync(
                        ProjectId);

            var existingRequirement =
                requirements.FirstOrDefault(r =>
                    r.CatalogId ==
                        catalog.CatalogId);

            int currentRequired =
                existingRequirement?.QuantityNeeded ??
                0;

            var qtyText =
                await Shell.Current.DisplayPromptAsync(
                    $"Add {catalog.CatalogName}",
                    $"How many more do you want to add?\n\n" +
                    $"Currently in this project: {currentRequired}\n" +
                    $"Available company-wide: {availableNow}",
                    "Add",
                    "Cancel",
                    keyboard:
                        Microsoft.Maui.Keyboard.Numeric,
                    initialValue:
                        "1");

            if (qtyText == null)
                return;

            if (!int.TryParse(
                    qtyText,
                    out int qty) ||
                qty <= 0)
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Quantity",
                    "Enter a valid quantity.",
                    "OK");

                return;
            }

            if (qty > availableNow)
            {
                await Shell.Current.DisplayAlert(
                    "Not Enough Available",
                    $"Only {availableNow} " +
                    $"{catalog.CatalogName} unit(s) " +
                    "are still available company-wide.",
                    "OK");

                return;
            }

            int newQuantity =
                currentRequired +
                qty;

            bool saved =
                await _firebase
                    .SetProjectEquipmentRequirementAsync(
                        ProjectId,
                        catalog.CatalogId,
                        catalog.CatalogName,
                        newQuantity);

            if (!saved)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    "Could not update the project equipment allocation.",
                    "OK");

                return;
            }

            await Shell.Current.DisplayAlert(
                "Equipment Added",
                $"{qty} {catalog.CatalogName} unit(s) added.\n\n" +
                $"Project total: {newQuantity}\n" +
                $"Company available remaining: " +
                $"{availableNow - qty}",
                "OK");

            await LoadAsync();
        }

        // ─────────────────────────────────────────────────────────
        // REMOVE EQUIPMENT ALLOCATION
        // ─────────────────────────────────────────────────────────

        private async Task RemoveEquipmentAsync(
            CatalogStockSummary item)
        {
            if (item == null)
                return;

            if (item.BorrowedCount > 0)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Remove",
                    $"{item.CatalogName} still has " +
                    $"{item.BorrowedCount} unit(s) " +
                    "under worker responsibility.\n\n" +
                    "Wait for them to be returned first.",
                    "OK");

                return;
            }

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Remove Equipment",
                    $"Remove {item.CatalogName} from " +
                    "this project's requirements?",
                    "Remove",
                    "Cancel");

            if (!confirm)
                return;

            await _firebase
                .RemoveProjectEquipmentRequirementAsync(
                    ProjectId,
                    item.CatalogId);

            await LoadAsync();
        }

        // ─────────────────────────────────────────────────────────
        // DISTRIBUTE
        // ─────────────────────────────────────────────────────────

        private async Task DistributeAsync(
            CatalogStockSummary item)
        {
            if (item == null)
                return;

            if (item.AvailableCount <= 0)
            {
                await Shell.Current.DisplayAlert(
                    "None Available",
                    $"All {item.CatalogName} allocated " +
                    "to this project are already distributed.",
                    "OK");

                return;
            }

            if (!HasWorkers)
            {
                await Shell.Current.DisplayAlert(
                    "No Workers",
                    "Assign workers to this project before " +
                    "distributing equipment.",
                    "OK");

                return;
            }

            var method =
                await Shell.Current.DisplayActionSheet(
                    $"Distribute {item.CatalogName}",
                    "Cancel",
                    null,
                    "Manual",
                    "Scan QR");

            if (method == "Manual")
            {
                await DistributeManualAsync(item);
            }
            else if (method == "Scan QR")
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(QrScannerView)}" +
                    $"?mode=Distribute" +
                    $"&projectId={ProjectId}" +
                    $"&catalogId={item.CatalogId}");
            }
        }

        // ─────────────────────────────────────────────────────────
        // MANUAL DISTRIBUTION
        // ─────────────────────────────────────────────────────────

        private async Task DistributeManualAsync(
            CatalogStockSummary item)
        {
            if (item == null ||
                IsBusy ||
                Project == null)
            {
                return;
            }

            var user =
                _auth.CurrentUser;

            if (user == null)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    "Current Project Engineer could not be identified.",
                    "OK");

                return;
            }

            int projectAvailable =
                item.AvailableCount;

            if (projectAvailable <= 0)
            {
                await Shell.Current.DisplayAlert(
                    "None Available",
                    $"All allocated {item.CatalogName} units " +
                    "for this project have already been distributed.",
                    "OK");

                return;
            }

            if (AssignedWorkers.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Workers",
                    "Assign workers to this project before " +
                    "distributing equipment.",
                    "OK");

                return;
            }

            var allTools =
                await _firebase.GetAllToolsAsync(
                    forceRefresh: true);

            var availableTools =
                allTools
                    .Where(t =>
                        t.CatalogId ==
                            item.CatalogId &&
                        t.Status ==
                            "Available")
                    .OrderBy(t =>
                        t.ToolId)
                    .ToList();

            if (availableTools.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "None Available",
                    $"No physical {item.CatalogName} units " +
                    "are currently available.",
                    "OK");

                return;
            }

            int maxCanDistribute =
                Math.Min(
                    projectAvailable,
                    availableTools.Count);

            var qtyText =
                await Shell.Current.DisplayPromptAsync(
                    $"Distribute {item.CatalogName}",
                    $"How many units do you want to distribute?\n\n" +
                    $"Available for this project: {projectAvailable}\n" +
                    $"Physical units available: {availableTools.Count}\n" +
                    $"Maximum: {maxCanDistribute}",
                    "Continue",
                    "Cancel",
                    keyboard:
                        Microsoft.Maui.Keyboard.Numeric,
                    initialValue:
                        "1");

            if (qtyText == null)
                return;

            if (!int.TryParse(
                    qtyText,
                    out int quantity) ||
                quantity <= 0)
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Quantity",
                    "Enter a valid quantity.",
                    "OK");

                return;
            }

            if (quantity > maxCanDistribute)
            {
                await Shell.Current.DisplayAlert(
                    "Not Enough Available",
                    $"You can only distribute up to " +
                    $"{maxCanDistribute} unit(s).",
                    "OK");

                return;
            }

            int distributed =
                0;

            for (int i = 1;
                 i <= quantity;
                 i++)
            {
                var toolIds =
                    availableTools
                        .Select(t =>
                            t.ToolId)
                        .ToArray();

                var selectedToolId =
                    await Shell.Current.DisplayActionSheet(
                        $"Tool {i} of {quantity} — Select Physical Unit",
                        "Stop",
                        null,
                        toolIds);

                if (string.IsNullOrWhiteSpace(selectedToolId) ||
                    selectedToolId == "Stop")
                {
                    break;
                }

                var tool =
                    availableTools.FirstOrDefault(t =>
                        t.ToolId ==
                        selectedToolId);

                if (tool == null)
                    continue;

                var workerNames =
                    AssignedWorkers
                        .Select(w =>
                            w.FullName)
                        .ToArray();

                var selectedWorkerName =
                    await Shell.Current.DisplayActionSheet(
                        $"{tool.ToolName} ({tool.ToolId}) — Assign To",
                        "Stop",
                        null,
                        workerNames);

                if (string.IsNullOrWhiteSpace(
                        selectedWorkerName) ||
                    selectedWorkerName ==
                        "Stop")
                {
                    break;
                }

                var worker =
                    AssignedWorkers.FirstOrDefault(w =>
                        w.FullName ==
                        selectedWorkerName);

                if (worker == null)
                    continue;

                var assignment =
                    new PreAssignment
                    {
                        ToolId =
                            tool.ToolId,

                        ToolName =
                            tool.ToolName,

                        WorkerId =
                            worker.UniqueKey,

                        WorkerName =
                            worker.FullName,

                        ProjectId =
                            ProjectId,

                        ProjectName =
                            Project.ProjectName,

                        AssignedById =
                            user.UniqueKey,

                        AssignedByName =
                            user.FullName,

                        Status =
                            "Pending",

                        DateCreated =
                            DateTime.Now
                    };

                bool success =
                    await _firebase
                        .CreatePreAssignmentAsync(
                            assignment);

                if (!success)
                {
                    await Shell.Current.DisplayAlert(
                        "Could Not Distribute",
                        $"{tool.ToolName} ({tool.ToolId}) " +
                        "could not be distributed.\n\n" +
                        "It may already have a pending assignment.",
                        "OK");

                    continue;
                }

                distributed++;

                availableTools.Remove(tool);
            }

            if (distributed > 0)
            {
                await Shell.Current.DisplayAlert(
                    "Distribution Sent",
                    $"{distributed} {item.CatalogName} unit(s) " +
                    "were distributed successfully.\n\n" +
                    "Each worker must confirm receipt before " +
                    "the equipment becomes Borrowed.",
                    "OK");
            }

            await LoadAsync();
        }
    }
}