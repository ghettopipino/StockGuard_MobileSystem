using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StockGuard.ViewModels
{
    [QueryProperty(nameof(ProjectId), "projectId")]
    public class ProjectDetailsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;
        private bool _isLoading;

        // ── Query Property ────────────────────────────────────────
        private string _projectId = string.Empty;
        public string ProjectId
        {
            get => _projectId;
            set
            {
                SetProperty(ref _projectId, value);
                if (!string.IsNullOrEmpty(value))
                    MainThread.BeginInvokeOnMainThread(
                        async () => await LoadAsync());
            }
        }

        // ── Project Data ──────────────────────────────────────────
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
                OnPropertyChanged(nameof(ShowReleaseSection));
            }
        }

        public string ProjectName =>
            Project?.ProjectName ?? "Loading...";
        public string Location =>
            Project?.Location ?? string.Empty;
        public string Status =>
            Project?.Status ?? string.Empty;
        public string StatusIcon =>
            Project?.StatusIcon ?? "❓";
        public string StatusColor =>
            Project?.StatusColor ?? "#94a3b8";
        public string StartDateLabel =>
            Project?.StartDateLabel ?? string.Empty;
        public string DurationLabel =>
            Project?.DurationLabel ?? string.Empty;
        public bool IsActive =>
            Project?.IsActive ?? false;

        // ── Theme ─────────────────────────────────────────────────
        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        // ── Collections ───────────────────────────────────────────
        public ObservableCollection<User>
            AssignedWorkers
        { get; } = new();

        // ── Equipment Summary ────────────────────────────────────
        public ObservableCollection<CatalogStockSummary>
            EquipmentSummary
        { get; } = new();

        // ── OnHold ────────────────────────────────────
        public ObservableCollection<Tool>
            OnHoldTools { get; } = new();

        // ── Stats ─────────────────────────────────────────────────
        private int _workerCount;
        public int WorkerCount
        {
            get => _workerCount;
            private set => SetProperty(ref _workerCount, value);
        }

        private int _toolCount;
        public int ToolCount
        {
            get => _toolCount;
            private set => SetProperty(ref _toolCount, value);
        }

        private int _borrowedCount;
        public int BorrowedCount
        {
            get => _borrowedCount;
            private set =>
                SetProperty(ref _borrowedCount, value);
        }

        // ── Empty States ──────────────────────────────────────────
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
        public bool NoWorkers => !HasWorkers;

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
        public bool NoEquipment => !HasEquipment;

        private bool _hasOnHoldTools;
        public bool HasOnHoldTools
        {
            get => _hasOnHoldTools;
            private set => SetProperty(ref _hasOnHoldTools, value);
        }

        public bool ShowReleaseSection =>
            Project?.Status == "Completed" && HasOnHoldTools;

        // ── Commands ──────────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AssignWorkerCommand { get; }
        public ICommand RemoveWorkerCommand { get; }
        public ICommand AssignEquipmentCommand { get; }
        public ICommand AddEquipmentCommand { get; }
        public ICommand RemoveEquipmentCommand { get; }
        public ICommand DistributeCommand { get; }
        public ICommand ReleaseToolCommand { get; }

        // ── Constructor ───────────────────────────────────────────
        public ProjectDetailsViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            GoBackCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            ToggleThemeCommand =
                new Command(() => _theme.Toggle());

            RefreshCommand = new Command(
                async () => await LoadAsync());

            AssignEquipmentCommand = new Command(
                async () => await AssignEquipmentAsync());

            AddEquipmentCommand = new Command(
                async () => await AddEquipmentAsync());

            RemoveEquipmentCommand = new Command<CatalogStockSummary>(
                async c => await RemoveEquipmentAsync(c));

            DistributeCommand = new Command<CatalogStockSummary>(
                async c => await DistributeAsync(c));

            AssignWorkerCommand = new Command(async () =>
                await Shell.Current.GoToAsync(
                    $"{nameof(BulkSelectView)}" +
                    $"?projectId={ProjectId}" +
                    $"&selectMode=workers"));

            RemoveWorkerCommand = new Command<User>(
                async u => await RemoveWorkerAsync(u));

            ReleaseToolCommand = new Command<Tool>(
                async tool => await ReleaseToolAsync(tool));
        }

        // ── Load Project Details ──────────────────────────────────
        public async Task LoadAsync()
        {
            if (string.IsNullOrEmpty(ProjectId)) return;
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                // Load project
                var projects =
                    await _firebase.GetAllProjectsAsync();
                Project = projects.FirstOrDefault(
                    p => p.ProjectId == ProjectId);

                if (Project is null) return;

                // ✅ Always clear before adding
                AssignedWorkers.Clear();

                // Load assigned workers
                var workerKeys = await _firebase
                    .GetProjectWorkerKeysAsync(ProjectId);

                var allUsers =
                    await _firebase.GetAllUsersAsync();

                foreach (var key in workerKeys)
                {
                    var worker = allUsers.FirstOrDefault(
                        u => u.UniqueKey == key);
                    if (worker != null)
                        AssignedWorkers.Add(worker);
                }

                HasWorkers = AssignedWorkers.Count > 0;
                WorkerCount = AssignedWorkers.Count;

                // Load equipment summary (also sets ToolCount/BorrowedCount)
                var allTools = await _firebase.GetAllToolsAsync();
                await LoadEquipmentSummaryAsync(allTools, workerKeys);

                OnHoldTools.Clear();

                var heldTools = allTools
                    .Where(t =>
                        t.Status == "OnHold" &&
                        t.HoldProjectId == ProjectId)
                    .OrderBy(t => t.ToolId)
                    .ToList();

                foreach (var tool in heldTools)
                    OnHoldTools.Add(tool);

                HasOnHoldTools = OnHoldTools.Count > 0;

                OnPropertyChanged(nameof(ShowReleaseSection));
                
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

        // ── Assign Worker (via BulkSelectView) ─────────────────────
        private async Task AssignWorkerAsync()
        {
            try
            {
                var allUsers =
                    await _firebase.GetAllUsersAsync();

                var assignedKeys = AssignedWorkers
                    .Select(w => w.UniqueKey).ToList();

                var available = allUsers
                    .Where(u =>
                        u.Role == "Worker" &&
                        u.AccountStatus == "Approved" &&
                        !assignedKeys.Contains(u.UniqueKey))
                    .ToList();

                if (available.Count == 0)
                {
                    await Shell.Current.DisplayAlert(
                        "No Workers Available",
                        "All approved workers are already " +
                        "assigned to this project.",
                        "OK");
                    return;
                }

                var names = available
                    .Select(w => w.FullName).ToArray();

                var selected =
                    await Shell.Current.DisplayActionSheet(
                        "Assign Worker",
                        "Cancel", null,
                        names);

                if (selected == null ||
                    selected == "Cancel") return;

                var worker = available.FirstOrDefault(
                    w => w.FullName == selected);

                if (worker is null) return;

                await _firebase.AssignWorkerToProjectAsync(
                    ProjectId, worker.UniqueKey);

                await Shell.Current.DisplayAlert(
                    "✅ Worker Assigned",
                    $"{worker.FullName} has been assigned " +
                    $"to {Project?.ProjectName}.",
                    "OK");

                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not assign worker.\n{ex.Message}",
                    "OK");
            }
        }

        // ── Remove Worker ─────────────────────────────────────────
        private async Task RemoveWorkerAsync(User worker)
        {
            if (worker is null) return;

            // CHANGED: was checking _allDeployedTools (now removed).
            // Live-check against all tools instead — same protection,
            // correct data source.
            var allTools = await _firebase.GetAllToolsAsync();
            var workerTools = allTools
                .Where(t => t.AssignedWorkerId == worker.UniqueKey &&
                            t.Status == "Borrowed")
                .ToList();

            if (workerTools.Count > 0)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Remove Worker",
                    $"{worker.FullName} still has " +
                    $"{workerTools.Count} tool(s) borrowed.\n\n" +
                    $"Return all tools first.",
                    "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Remove Worker",
                $"Remove {worker.FullName} from " +
                $"{Project?.ProjectName}?",
                "Remove", "Cancel");

            if (!confirm) return;

            try
            {
                await _firebase.RemoveWorkerFromProjectAsync(
                    ProjectId, worker.UniqueKey);

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
                    $"Could not remove worker.\n{ex.Message}",
                    "OK");
            }
        }

        // ── Assign Equipment via QR Scan ────────────────────────────
        private async Task AssignEquipmentAsync()
        {
            await Shell.Current.GoToAsync(
                $"{nameof(QrScannerView)}" +
                $"?mode=AssignEquipment" +
                $"&projectId={ProjectId}");
        }

        private async Task LoadEquipmentSummaryAsync(List<Tool> allTools, List<string> workerKeys)
        {
            var requirements = await _firebase.GetProjectEquipmentRequirementsAsync(ProjectId);

            EquipmentSummary.Clear();
            foreach (var req in requirements)
            {
                var borrowedCount = allTools.Count(t =>
                    t.CatalogId == req.CatalogId &&
                    t.Status == "Borrowed" &&
                     t.BorrowedProjectId == ProjectId);

                EquipmentSummary.Add(new CatalogStockSummary
                {
                    CatalogId = req.CatalogId,
                    CatalogName = req.CatalogName,
                    QuantityNeeded = req.QuantityNeeded,
                    BorrowedCount = borrowedCount
                });
            }

            ToolCount = requirements.Sum(r => r.QuantityNeeded);
            BorrowedCount = EquipmentSummary.Sum(e => e.BorrowedCount);
            HasEquipment = EquipmentSummary.Count > 0;
        }

        private async Task AddEquipmentAsync()
        {
            var catalogs = await _firebase.GetAllCatalogsAsync();

            if (catalogs.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Catalog Items",
                    "Create equipment catalog entries first.",
                    "OK");

                return;
            }

            // ── SEARCH EQUIPMENT ─────────────────────────────────────
            var search = await Shell.Current.DisplayPromptAsync(
                "Find Equipment",
                "Search for the equipment you want to add:",
                "Search",
                "Cancel",
                placeholder: "e.g. Power Drill");

            if (search == null)
                return;

            search = search.Trim();

            var matches = string.IsNullOrWhiteSpace(search)
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

            // If only one result, use it directly
            if (matches.Count == 1)
            {
                catalog = matches[0];
            }
            else
            {
                var names = matches
                    .Select(c => c.CatalogName)
                    .ToArray();

                var selected = await Shell.Current.DisplayActionSheet(
                    "Select Equipment",
                    "Cancel",
                    null,
                    names);

                if (selected == null ||
                    selected == "Cancel")
                    return;

                catalog = matches.FirstOrDefault(
                    c => c.CatalogName == selected);
            }

            if (catalog == null)
                return;

            // ── GET TRUE COMPANY AVAILABILITY ────────────────────────

            var allTools = await _firebase.GetAllToolsAsync(
                forceRefresh: true);

            var allocations = await _firebase
                .GetAllActiveProjectEquipmentRequirementsAsync();

            // Physical tools still marked Available
            int physicalAvailable = allTools.Count(t =>
                t.CatalogId == catalog.CatalogId &&
                t.Status == "Available");

            // Actual physical tools already borrowed by workers
            int actualBorrowed = allTools.Count(t =>
                t.CatalogId == catalog.CatalogId &&
                t.Status == "Borrowed");

            // Total project allocations for this catalog
            int totalAllocated = allocations
                .Where(a =>
                    a.CatalogId == catalog.CatalogId)
                .Sum(a => a.QuantityNeeded);

            // Borrowed physical tools already consume
            // part of the project allocation
            int remainingReserved = Math.Max(
                0,
                totalAllocated - actualBorrowed);

            // True quantity still available at company/shop level
            int availableNow = Math.Max(
                0,
                physicalAvailable - remainingReserved);

            if (availableNow <= 0)
            {
                await Shell.Current.DisplayAlert(
                    "None Available",
                    $"There are currently no available " +
                    $"{catalog.CatalogName} units remaining.",
                    "OK");

                return;
            }

            // ── CURRENT PROJECT QUANTITY ──────────────────────────────

            var requirements = await _firebase
                .GetProjectEquipmentRequirementsAsync(ProjectId);

            var existingRequirement =
                requirements.FirstOrDefault(r =>
                    r.CatalogId == catalog.CatalogId);

            int currentRequired =
                existingRequirement?.QuantityNeeded ?? 0;

            // ── ASK QUANTITY ──────────────────────────────────────────

            var qtyText = await Shell.Current.DisplayPromptAsync(
                $"Add {catalog.CatalogName}",
                $"How many more do you want to add?\n\n" +
                $"Currently in this project: {currentRequired}\n" +
                $"Available company-wide: {availableNow}",
                "Add",
                "Cancel",
                keyboard: Microsoft.Maui.Keyboard.Numeric,
                initialValue: "1");

            if (qtyText == null)
                return;

            if (!int.TryParse(qtyText, out var qty) ||
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
                    $"are still available company-wide.",
                    "OK");

                return;
            }

            // ── SAVE ALLOCATION ───────────────────────────────────────

            int newQuantity =
                currentRequired + qty;

            bool saved = await _firebase
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
                $"Company available remaining: {availableNow - qty}",
                "OK");

            await LoadAsync();
        }

        private async Task RemoveEquipmentAsync(CatalogStockSummary item)
        {
            if (item is null) return;

            if (item.BorrowedCount > 0)
            {
                await Shell.Current.DisplayAlert("Cannot Remove",
                    $"{item.CatalogName} still has {item.BorrowedCount} unit(s) borrowed " +
                    $"on this project.\n\nWait for them to be returned first.", "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert("Remove Equipment",
                $"Remove {item.CatalogName} from this project's requirements?",
                "Remove", "Cancel");
            if (!confirm) return;

            await _firebase.RemoveProjectEquipmentRequirementAsync(ProjectId, item.CatalogId);
            await LoadAsync();
        }

        private async Task DistributeAsync(CatalogStockSummary item)
        {
            if (item is null) return;

            if (item.AvailableCount <= 0)
            {
                await Shell.Current.DisplayAlert("None Available",
                    $"All {item.CatalogName} allocated to this project are currently borrowed.", "OK");
                return;
            }

            if (!HasWorkers)
            {
                await Shell.Current.DisplayAlert("No Workers",
                    "Assign workers to this project before distributing equipment.", "OK");
                return;
            }

            var method = await Shell.Current.DisplayActionSheet(
                $"Distribute {item.CatalogName}", "Cancel", null, "Manual", "Scan QR");

            if (method == "Manual")
                await DistributeManualAsync(item);
            else if (method == "Scan QR")
                await Shell.Current.GoToAsync(
                    $"{nameof(QrScannerView)}?mode=Distribute&projectId={ProjectId}&catalogId={item.CatalogId}");
        }

        private async Task DistributeManualAsync(CatalogStockSummary item)
        {
            var allTools = await _firebase.GetAllToolsAsync(
                forceRefresh: true);

            var available = allTools
                .Where(t =>
                    t.CatalogId == item.CatalogId &&
                    t.Status == "Available")
                .ToList();

            if (available.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "None Available",
                    $"No available units of {item.CatalogName} in inventory.",
                    "OK");

                return;
            }

            var toolIds = available
                .Select(t => t.ToolId)
                .ToArray();

            var selectedToolId =
                await Shell.Current.DisplayActionSheet(
                    $"Select {item.CatalogName} unit:",
                    "Cancel",
                    null,
                    toolIds);

            if (selectedToolId == null ||
                selectedToolId == "Cancel")
                return;

            var tool = available.FirstOrDefault(
                t => t.ToolId == selectedToolId);

            if (tool is null)
                return;

            var workerNames = AssignedWorkers
                .Select(w => w.FullName)
                .ToArray();

            var selectedWorkerName =
                await Shell.Current.DisplayActionSheet(
                    $"Distribute {tool.ToolName} ({tool.ToolId}) to:",
                    "Cancel",
                    null,
                    workerNames);

            if (selectedWorkerName == null ||
                selectedWorkerName == "Cancel")
                return;

            var worker = AssignedWorkers.FirstOrDefault(
                w => w.FullName == selectedWorkerName);

            if (worker is null)
                return;

            var assignment = new PreAssignment
            {
                ToolId = tool.ToolId,
                ToolName = tool.ToolName,

                WorkerId = worker.UniqueKey,
                WorkerName = worker.FullName,

                ProjectId = ProjectId,
                ProjectName = Project?.ProjectName ?? string.Empty,

                AssignedByName =
                    _auth.CurrentUser?.FullName ?? "Project Engineer",

                Status = "Pending",
                DateCreated = DateTime.Now
            };

            bool success =
                await _firebase.CreatePreAssignmentAsync(
                    assignment);

            if (!success)
            {
                await Shell.Current.DisplayAlert(
                    "Could Not Distribute",
                    $"{tool.ToolName} could not be distributed.\n\n" +
                    $"It may already have a pending distribution.",
                    "OK");

                await LoadAsync();
                return;
            }

            await Shell.Current.DisplayAlert(
                "✅ Distribution Sent",
                $"{tool.ToolName} ({tool.ToolId}) was distributed " +
                $"to {worker.FullName}.\n\n" +
                $"The tool will remain Available until " +
                $"{worker.FullName} accepts it.",
                "OK");

            await LoadAsync();
        }

        private async Task ReleaseToolAsync(Tool tool)
        {
            if (tool is null || IsBusy)
                return;

            if (Project is null || Project.Status != "Completed")
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Release",
                    "Equipment can only be released from a completed project.",
                    "OK");

                return;
            }

            if (!tool.IsOnHold)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Release",
                    "Only equipment currently On Hold can be released.",
                    "OK");

                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Release Equipment",
                $"Release {tool.ToolName} ({tool.ToolId})?\n\n" +
                $"Project: {tool.HoldProjectName}\n" +
                $"Location: {tool.HoldLocation}\n" +
                $"Last Borrower: {tool.LastBorrowerName}\n\n" +
                $"The equipment will become Available.",
                "Release",
                "Cancel");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                var user = _auth.CurrentUser!;

                // Save details before clearing them
                var holdProjectId = tool.HoldProjectId;
                var holdProjectName = tool.HoldProjectName;
                var holdLocation = tool.HoldLocation;
                var lastBorrowerId = tool.LastBorrowerId;
                var lastBorrowerName = tool.LastBorrowerName;

                // Release tool
                tool.Status = "Available";

                tool.HoldProjectId = string.Empty;
                tool.HoldProjectName = string.Empty;
                tool.HoldLocation = string.Empty;

                tool.LastBorrowerId = string.Empty;
                tool.LastBorrowerName = string.Empty;
                tool.HoldDate = null;

                tool.AssignedWorkerId = string.Empty;
                tool.AssignedWorkerName = string.Empty;

                tool.BorrowedProjectId = string.Empty;
                tool.BorrowedProjectName = string.Empty;

                tool.BorrowDate = null;

                var success = await _firebase.UpdateToolAsync(tool);

                if (!success)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not release the equipment.",
                        "OK");

                    return;
                }

                // Audit trail
                await _firebase.LogTransactionAsync(
                    new TransactionLog
                    {
                        ToolId = tool.ToolId,
                        ToolName = tool.ToolName,

                        WorkerId = lastBorrowerId,
                        WorkerName = lastBorrowerName,

                        ProjectId = holdProjectId,
                        ProjectName = holdProjectName,

                        Action = "Released",

                        Description =
                            $"Released from completed project by {user.FullName}. " +
                            $"Previous hold location: {holdLocation}.",

                        Condition = tool.Condition,
                        Date = DateTime.Now
                    });

                await Shell.Current.DisplayAlert(
                    "✅ Equipment Released",
                    $"{tool.ToolName} ({tool.ToolId}) is now Available.",
                    "OK");

                await LoadAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}