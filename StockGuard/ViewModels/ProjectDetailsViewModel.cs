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
        // QUERY
        // ─────────────────────────────────────────────────────────

        private string _projectId =
            string.Empty;

        public string ProjectId
        {
            get => _projectId;

            set
            {
                SetProperty(
                    ref _projectId,
                    value);

                if (!string.IsNullOrWhiteSpace(
                        value))
                {
                    MainThread.BeginInvokeOnMainThread(
                        async () =>
                            await LoadAsync());
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
                SetProperty(
                    ref _project,
                    value);

                OnPropertyChanged(
                    nameof(ProjectName));

                OnPropertyChanged(
                    nameof(Location));

                OnPropertyChanged(
                    nameof(Status));

                OnPropertyChanged(
                    nameof(StatusIcon));

                OnPropertyChanged(
                    nameof(StatusColor));

                OnPropertyChanged(
                    nameof(StartDateLabel));

                OnPropertyChanged(
                    nameof(DurationLabel));

                OnPropertyChanged(
                    nameof(IsActive));
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
            Project?.IsActive ??
            false;


        // ─────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────

        public string ThemeIcon =>
            _theme.IsDark
                ? "🌙"
                : "☀️";


        // ─────────────────────────────────────────────────────────
        // COLLECTIONS
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<User>
            AssignedWorkers
        { get; } = new();


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
                SetProperty(
                    ref _hasWorkers,
                    value);

                OnPropertyChanged(
                    nameof(NoWorkers));
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
                SetProperty(
                    ref _hasEquipment,
                    value);

                OnPropertyChanged(
                    nameof(NoEquipment));
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
            _firebase =
                firebase;

            _auth =
                auth;

            _theme =
                theme;


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
                    () =>
                        _theme.Toggle());


            RefreshCommand =
                new Command(
                    async () =>
                        await LoadAsync());


            AssignEquipmentCommand =
                new Command<CatalogStockSummary>(
                    async item =>
                        await BorrowEquipmentAsync(
                            item));


            AddEquipmentCommand =
                new Command(
                    async () =>
                        await AddEquipmentAsync());


            RemoveEquipmentCommand =
                new Command<CatalogStockSummary>(
                    async item =>
                        await RemoveEquipmentAsync(
                            item));


            DistributeCommand =
                new Command<CatalogStockSummary>(
                    async item =>
                        await DistributeAsync(
                            item));


            AssignWorkerCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(
                                $"{nameof(BulkSelectView)}" +
                                $"?projectId={ProjectId}" +
                                $"&selectMode=workers"));


            RemoveWorkerCommand =
                new Command<User>(
                    async worker =>
                        await RemoveWorkerAsync(
                            worker));
        }


        // ─────────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────────

        public async Task LoadAsync()
        {
            if (string.IsNullOrWhiteSpace(
                    ProjectId))
            {
                return;
            }

            if (_isLoading)
                return;


            _isLoading =
                true;

            IsBusy =
                true;


            try
            {
                var projects =
                    await _firebase
                        .GetAllProjectsAsync();

                Project =
                    projects.FirstOrDefault(p =>
                        p.ProjectId ==
                        ProjectId);

                if (Project == null)
                    return;


                // ── WORKERS ───────────────────────────────

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
                            u.UniqueKey ==
                            key);

                    if (worker != null)
                    {
                        AssignedWorkers.Add(
                            worker);
                    }
                }


                HasWorkers =
                    AssignedWorkers.Count >
                    0;

                WorkerCount =
                    AssignedWorkers.Count;


                // ── EQUIPMENT ─────────────────────────────

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
                    $"LoadProject error: " +
                    $"{ex.Message}");
            }
            finally
            {
                IsBusy =
                    false;

                _isLoading =
                    false;
            }
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
                var projectTools =
                    allTools
                        .Where(t =>
                            string.Equals(
                                t.CatalogId,
                                req.CatalogId,
                                StringComparison.OrdinalIgnoreCase) &&

                            string.Equals(
                                t.BorrowedProjectId,
                                ProjectId,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();


                int borrowedCount =
                    projectTools.Count;


                int distributedCount =
                    projectTools.Count(t =>
                        !string.IsNullOrWhiteSpace(
                            t.AssignedWorkerId));


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
                            borrowedCount,

                        DistributedCount =
                            distributedCount
                    });
            }


            // Requirement total.
            ToolCount =
                requirements.Sum(r =>
                    r.QuantityNeeded);


            // Actual physical equipment borrowed
            // from the office.
            BorrowedCount =
                EquipmentSummary.Sum(e =>
                    e.BorrowedCount);


            HasEquipment =
                EquipmentSummary.Count >
                0;
        }


        // ─────────────────────────────────────────────────────────
        // ADD EQUIPMENT REQUIREMENT
        // ─────────────────────────────────────────────────────────

        private async Task AddEquipmentAsync()
        {
            var catalogs =
                await _firebase
                    .GetAllCatalogsAsync();

            if (catalogs.Count ==
                0)
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
                    "Search for equipment required by this project:",
                    "Search",
                    "Cancel",
                    placeholder:
                        "e.g. Power Drill");


            if (search == null)
                return;


            search =
                search.Trim();


            var matches =
                string.IsNullOrWhiteSpace(
                    search)
                    ? catalogs

                    : catalogs
                        .Where(c =>
                            c.CatalogName.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();


            if (matches.Count ==
                0)
            {
                await Shell.Current.DisplayAlert(
                    "No Match Found",
                    $"No equipment matched \"{search}\".",
                    "OK");

                return;
            }


            EquipmentCatalog? catalog =
                null;


            if (matches.Count ==
                1)
            {
                catalog =
                    matches[0];
            }
            else
            {
                var names =
                    matches
                        .Select(c =>
                            c.CatalogName)
                        .ToArray();


                var selected =
                    await Shell.Current.DisplayActionSheet(
                        "Select Equipment",
                        "Cancel",
                        null,
                        names);


                if (string.IsNullOrWhiteSpace(
                        selected) ||
                    selected ==
                        "Cancel")
                {
                    return;
                }


                catalog =
                    matches.FirstOrDefault(c =>
                        c.CatalogName ==
                        selected);
            }


            if (catalog ==
                null)
            {
                return;
            }


            var allTools =
                await _firebase
                    .GetAllToolsAsync(
                        forceRefresh: true);


            int totalPhysical =
                allTools.Count(t =>
                    t.CatalogId ==
                    catalog.CatalogId);


            int currentlyAvailable =
                allTools.Count(t =>
                    t.CatalogId ==
                        catalog.CatalogId &&
                    t.Status ==
                        "Available");


            if (totalPhysical <=
                0)
            {
                await Shell.Current.DisplayAlert(
                    "No Physical Equipment",
                    $"There are no physical {catalog.CatalogName} " +
                    "units registered.",
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
                    $"How many more does this project require?\n\n" +
                    $"Currently required: {currentRequired}\n" +
                    $"Registered company-wide: {totalPhysical}\n" +
                    $"Currently available in office: {currentlyAvailable}\n\n" +
                    "Adding a requirement does not borrow the physical equipment.",
                    "Add",
                    "Cancel",
                    keyboard:
                        Microsoft.Maui.Keyboard.Numeric,
                    initialValue:
                        "1");


            if (qtyText ==
                null)
            {
                return;
            }


            if (!int.TryParse(
                    qtyText,
                    out int qty) ||
                qty <=
                    0)
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Quantity",
                    "Enter a valid quantity.",
                    "OK");

                return;
            }


            int newQuantity =
                currentRequired +
                qty;


            if (newQuantity >
                totalPhysical)
            {
                await Shell.Current.DisplayAlert(
                    "Requirement Too High",
                    $"Only {totalPhysical} physical " +
                    $"{catalog.CatalogName} unit(s) are registered " +
                    "company-wide.",
                    "OK");

                return;
            }


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
                    "Could not update the project equipment requirement.",
                    "OK");

                return;
            }


            await Shell.Current.DisplayAlert(
                "Requirement Added",
                $"{catalog.CatalogName}\n\n" +
                $"Required by project: {newQuantity}\n" +
                $"Currently available in office: {currentlyAvailable}\n\n" +
                "Use Borrow to scan or manually select the physical equipment.",
                "OK");


            await LoadAsync();
        }


        // ─────────────────────────────────────────────────────────
        // BORROW PHYSICAL EQUIPMENT
        // ─────────────────────────────────────────────────────────

        private async Task BorrowEquipmentAsync(
            CatalogStockSummary item)
        {
            if (item == null ||
                Project == null)
            {
                return;
            }


            if (item.RemainingCount <=
                0)
            {
                await Shell.Current.DisplayAlert(
                    "Requirement Fulfilled",
                    $"This project already has all " +
                    $"{item.QuantityNeeded} required " +
                    $"{item.CatalogName} unit(s).",
                    "OK");

                return;
            }


            var method =
                await Shell.Current.DisplayActionSheet(
                    $"Borrow {item.CatalogName}",
                    "Cancel",
                    null,
                    "Scan QR",
                    "Manual Select");


            if (method ==
                "Scan QR")
            {
                await Shell.Current
                    .GoToAsync(
                        $"{nameof(QrScannerView)}" +
                        $"?mode=AssignEquipment" +
                        $"&projectId={ProjectId}" +
                        $"&catalogId={item.CatalogId}");

                return;
            }


            if (method ==
                "Manual Select")
            {
                await BorrowEquipmentManualAsync(
                    item);
            }
        }


        // ─────────────────────────────────────────────────────────
        // MANUAL BORROW
        // ─────────────────────────────────────────────────────────

        private async Task BorrowEquipmentManualAsync(
            CatalogStockSummary item)
        {
            if (Project ==
                null)
            {
                return;
            }


            var currentPE =
                _auth.CurrentUser;

            if (currentPE ==
                null)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    "Current Project Engineer could not be identified.",
                    "OK");

                return;
            }


            var allTools =
                await _firebase
                    .GetAllToolsAsync(
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


            if (availableTools.Count ==
                0)
            {
                await Shell.Current.DisplayAlert(
                    "None Available",
                    $"No physical {item.CatalogName} units " +
                    "are currently available in the office.",
                    "OK");

                return;
            }


            var options =
                availableTools
                    .Select(t =>
                        $"{t.ToolId} — {t.ToolName}")
                    .ToArray();


            var selected =
                await Shell.Current.DisplayActionSheet(
                    $"Select {item.CatalogName}",
                    "Cancel",
                    null,
                    options);


            if (string.IsNullOrWhiteSpace(
                    selected) ||
                selected ==
                    "Cancel")
            {
                return;
            }


            int selectedIndex =
                Array.IndexOf(
                    options,
                    selected);


            if (selectedIndex <
                0)
            {
                return;
            }


            var tool =
                availableTools[
                    selectedIndex];


            string result =
                await _firebase
                    .BorrowToolIntoProjectAsync(
                        tool.ToolId,
                        ProjectId,
                        currentPE.UniqueKey,
                        currentPE.FullName);


            if (result ==
                "SUCCESS")
            {
                await Shell.Current.DisplayAlert(
                    "Equipment Borrowed",
                    $"{tool.ToolName} ({tool.ToolId}) is now Borrowed.\n\n" +
                    $"Project: {Project.ProjectName}\n" +
                    $"Accountability: Project Engineer",
                    "OK");

                await LoadAsync();

                return;
            }


            if (result ==
                "REQUIREMENT_FULFILLED")
            {
                await Shell.Current.DisplayAlert(
                    "Requirement Fulfilled",
                    $"This project already has all required " +
                    $"{item.CatalogName} units.",
                    "OK");

                await LoadAsync();

                return;
            }


            if (result ==
                "NOT_AVAILABLE")
            {
                await Shell.Current.DisplayAlert(
                    "Not Available",
                    $"{tool.ToolName} ({tool.ToolId}) is no longer available.",
                    "OK");

                await LoadAsync();

                return;
            }


            await Shell.Current.DisplayAlert(
                "Error",
                "Could not borrow the selected equipment.",
                "OK");
        }


        // ─────────────────────────────────────────────────────────
        // DISTRIBUTE
        // ─────────────────────────────────────────────────────────

        private async Task DistributeAsync(
            CatalogStockSummary item)
        {
            if (item ==
                null)
            {
                return;
            }


            if (item.WithPECount <=
                0)
            {
                await Shell.Current.DisplayAlert(
                    "Nothing to Distribute",
                    $"There are no {item.CatalogName} units currently " +
                    "under Project Engineer accountability.\n\n" +
                    "Borrow equipment from the office first.",
                    "OK");

                return;
            }


            if (!HasWorkers)
            {
                await Shell.Current.DisplayAlert(
                    "No Workers",
                    "Assign workers to this project before distributing equipment.",
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


            if (method ==
                "Manual")
            {
                await DistributeManualAsync(
                    item);
            }
            else if (method ==
                "Scan QR")
            {
                await Shell.Current
                    .GoToAsync(
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
            if (item ==
                    null ||
                Project ==
                    null)
            {
                return;
            }


            var allTools =
                await _firebase
                    .GetAllToolsAsync(
                        forceRefresh: true);


            // IMPORTANT:
            // Only tools already borrowed into this project.
            var projectBorrowedTools =
                allTools
                    .Where(t =>
                        t.CatalogId ==
                            item.CatalogId &&

                        t.Status ==
                            "Borrowed" &&

                        t.BorrowedProjectId ==
                            ProjectId &&

                        string.IsNullOrWhiteSpace(
                            t.AssignedWorkerId))
                    .OrderBy(t =>
                        t.ToolId)
                    .ToList();


            if (projectBorrowedTools.Count ==
                0)
            {
                await Shell.Current.DisplayAlert(
                    "Nothing to Distribute",
                    $"No borrowed {item.CatalogName} units are " +
                    "currently under Project Engineer accountability.",
                    "OK");

                return;
            }


            var toolOptions =
                projectBorrowedTools
                    .Select(t =>
                        $"{t.ToolId} — {t.ToolName}")
                    .ToArray();


            var selected =
                await Shell.Current.DisplayActionSheet(
                    $"Select {item.CatalogName}",
                    "Cancel",
                    null,
                    toolOptions);


            if (string.IsNullOrWhiteSpace(
                    selected) ||
                selected ==
                    "Cancel")
            {
                return;
            }


            int index =
                Array.IndexOf(
                    toolOptions,
                    selected);


            if (index <
                0)
            {
                return;
            }


            var tool =
                projectBorrowedTools[
                    index];


            bool assigned =
                await WorkerAssignmentHelper
                    .AssignToolToWorkerViaPickerAsync(
                        _firebase,
                        _auth,
                        tool,
                        ProjectId);


            if (assigned)
            {
                await LoadAsync();
            }
        }


        // ─────────────────────────────────────────────────────────
        // REMOVE EQUIPMENT REQUIREMENT
        // ─────────────────────────────────────────────────────────

        private async Task RemoveEquipmentAsync(
            CatalogStockSummary item)
        {
            if (item ==
                null)
            {
                return;
            }


            if (item.BorrowedCount >
                0)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Remove",
                    $"{item.CatalogName} still has " +
                    $"{item.BorrowedCount} physical unit(s) " +
                    "borrowed under this project.\n\n" +
                    "Return the physical equipment first.",
                    "OK");

                return;
            }


            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Remove Equipment",
                    $"Remove {item.CatalogName} from this project's requirements?",
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
        // REMOVE WORKER
        // ─────────────────────────────────────────────────────────

        private async Task RemoveWorkerAsync(
            User worker)
        {
            if (worker ==
                null)
            {
                return;
            }


            var allTools =
                await _firebase
                    .GetAllToolsAsync(
                        forceRefresh: true);


            var workerTools =
                allTools
                    .Where(t =>
                        t.AssignedWorkerId ==
                            worker.UniqueKey &&
                        (
                            t.Status ==
                                "Borrowed" ||
                            t.Status ==
                                "PendingReturn"
                        ))
                    .ToList();


            if (workerTools.Count >
                0)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Remove Worker",
                    $"{worker.FullName} still has " +
                    $"{workerTools.Count} equipment item(s) " +
                    "under their responsibility.\n\n" +
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
    }
}