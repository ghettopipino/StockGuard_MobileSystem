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
                await Shell.Current.DisplayAlert("No Catalog Items",
                    "Create equipment catalog entries first.", "OK");
                return;
            }

            var names = catalogs.Select(c => c.CatalogName).ToArray();
            var selected = await Shell.Current.DisplayActionSheet(
                "Add Equipment to Project", "Cancel", null, names);
            if (selected == null || selected == "Cancel") return;

            var catalog = catalogs.FirstOrDefault(c => c.CatalogName == selected);
            if (catalog is null) return;

            // ── Real available count, company-wide ─────────────────────
            var allTools = await _firebase.GetAllToolsAsync();
            var availableNow = allTools.Count(t =>
                t.CatalogId == catalog.CatalogId && t.Status == "Available");

            if (availableNow == 0)
            {
                await Shell.Current.DisplayAlert("None Available",
                    $"All {catalog.CatalogName} units are currently borrowed elsewhere.", "OK");
                return;
            }

            var qtyText = await Shell.Current.DisplayPromptAsync(
                "Quantity Needed",
                $"How many {catalog.CatalogName} does this project need?\n\n" +
                $"({availableNow} currently available company-wide)",
                "Save", "Cancel",
                keyboard: Microsoft.Maui.Keyboard.Numeric, initialValue: "1");

            if (qtyText == null) return;

            if (!int.TryParse(qtyText, out var qty) || qty <= 0)
            {
                await Shell.Current.DisplayAlert("Invalid", "Enter a valid quantity.", "OK");
                return;
            }

            if (qty > availableNow)
            {
                await Shell.Current.DisplayAlert("Not Enough Available",
                    $"Only {availableNow} {catalog.CatalogName} are currently available " +
                    $"company-wide. Enter {availableNow} or fewer.", "OK");
                return;
            }

            await _firebase.SetProjectEquipmentRequirementAsync(
                ProjectId, catalog.CatalogId, catalog.CatalogName, qty);

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
            var allTools = await _firebase.GetAllToolsAsync();
            var available = allTools
                .Where(t => t.CatalogId == item.CatalogId && t.Status == "Available")
                .ToList();

            if (available.Count == 0)
            {
                await Shell.Current.DisplayAlert("None Available",
                    $"No available units of {item.CatalogName} in inventory.", "OK");
                return;
            }

            var toolIds = available.Select(t => t.ToolId).ToArray();
            var selectedToolId = await Shell.Current.DisplayActionSheet(
                $"Select {item.CatalogName} unit:", "Cancel", null, toolIds);
            if (selectedToolId == null || selectedToolId == "Cancel") return;

            var tool = available.FirstOrDefault(t => t.ToolId == selectedToolId);
            if (tool is null) return;

            var workerNames = AssignedWorkers.Select(w => w.FullName).ToArray();
            var selectedWorkerName = await Shell.Current.DisplayActionSheet(
                $"Assign {tool.ToolName} ({tool.ToolId}) to:", "Cancel", null, workerNames);
            if (selectedWorkerName == null || selectedWorkerName == "Cancel") return;

            var worker = AssignedWorkers.FirstOrDefault(w => w.FullName == selectedWorkerName);
            if (worker is null) return;

            bool success = await _firebase.BorrowToolForProjectAsync(
                tool.ToolId, tool.ToolName, worker.UniqueKey, worker.FullName,
                ProjectId, Project?.ProjectName ?? string.Empty,
                _auth.CurrentUser?.FullName ?? "Project Engineer");

            if (!success)
            {
                await Shell.Current.DisplayAlert("Error",
                    $"Could not assign {tool.ToolName} — it may have just been " +
                    $"borrowed by someone else. Please try again.", "OK");
                await LoadAsync();
                return;
            }

            await Shell.Current.DisplayAlert("✅ Distributed",
                $"{tool.ToolName} ({tool.ToolId}) is now assigned to {worker.FullName}.", "OK");

            await LoadAsync();
        }
    }
}