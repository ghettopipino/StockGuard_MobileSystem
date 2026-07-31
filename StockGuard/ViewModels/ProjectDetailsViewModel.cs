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

        public ObservableCollection<Tool>
            DeployedTools
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

        private bool _hasTools;
        public bool HasTools
        {
            get => _hasTools;
            private set
            {
                SetProperty(ref _hasTools, value);
                OnPropertyChanged(nameof(NoTools));
            }
        }
        public bool NoTools => !HasTools;

        // ── Tools: search + pagination ─────────────────────────────
        private const int ToolsPageSize = 5;
        private readonly List<Tool> _allDeployedTools = new();

        private string _toolSearchText = string.Empty;
        public string ToolSearchText
        {
            get => _toolSearchText;
            set
            {
                SetProperty(ref _toolSearchText, value);
                _toolsPage = 1;
                ApplyToolsFilterAndPage();
            }
        }

        private int _toolsPage = 1;
        public int ToolsPage
        {
            get => _toolsPage;
            private set
            {
                SetProperty(ref _toolsPage, value);
                OnPropertyChanged(nameof(ToolsPageLabel));
                OnPropertyChanged(nameof(CanGoToolsPrev));
                OnPropertyChanged(nameof(CanGoToolsNext));
                OnPropertyChanged(nameof(ShowToolsPager));
            }
        }

        private int _toolsTotalPages = 1;
        public int ToolsTotalPages
        {
            get => _toolsTotalPages;
            private set
            {
                SetProperty(ref _toolsTotalPages, value);
                OnPropertyChanged(nameof(ToolsPageLabel));
                OnPropertyChanged(nameof(CanGoToolsPrev));
                OnPropertyChanged(nameof(CanGoToolsNext));
                OnPropertyChanged(nameof(ShowToolsPager));
            }
        }

        public string ToolsPageLabel => $"{ToolsPage} / {ToolsTotalPages}";
        public bool CanGoToolsPrev => ToolsPage > 1;
        public bool CanGoToolsNext => ToolsPage < ToolsTotalPages;
        public bool ShowToolsPager => ToolsTotalPages > 1;

        // ── Commands ──────────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AssignWorkerCommand { get; }
        public ICommand RemoveWorkerCommand { get; }
        public ICommand DeployToolCommand { get; }
        public ICommand RemoveToolCommand { get; }
        public ICommand PreAssignToolCommand { get; }
        public ICommand AssignEquipmentCommand { get; }
        public ICommand DeployViaScanCommand { get; }
        public ICommand ToolsPrevCommand { get; }
        public ICommand ToolsNextCommand { get; }


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
            PreAssignToolCommand = new Command<Tool>(
               async t => await PreAssignToolAsync(t));

            AssignEquipmentCommand = new Command(
                async () => await AssignEquipmentAsync());

            DeployViaScanCommand = new Command(async () =>
                await Shell.Current.GoToAsync(
                    $"{nameof(QrScannerView)}?mode=Deploy&projectId={ProjectId}"));

            AssignWorkerCommand = new Command(async () =>
    await Shell.Current.GoToAsync(
        $"{nameof(BulkSelectView)}" +
        $"?projectId={ProjectId}" +
        $"&selectMode=workers"));

            RemoveWorkerCommand = new Command<User>(
                async u => await RemoveWorkerAsync(u));

            DeployToolCommand = new Command(async () =>
    await Shell.Current.GoToAsync(
        $"{nameof(BulkSelectView)}" +
        $"?projectId={ProjectId}" +
        $"&selectMode=tools"));

            RemoveToolCommand = new Command<Tool>(
                async t => await RemoveToolAsync(t));

            ToolsPrevCommand = new Command(() =>
            {
                if (!CanGoToolsPrev) return;
                _toolsPage--;
                ApplyToolsFilterAndPage();
            });

            ToolsNextCommand = new Command(() =>
            {
                if (!CanGoToolsNext) return;
                _toolsPage++;
                ApplyToolsFilterAndPage();
            });
        }
        private async Task PreAssignToolAsync(Tool tool)
        {
            if (tool is null) return;

            var workerKeys = await _firebase
                .GetProjectWorkerKeysAsync(ProjectId);

            var allUsers =
                await _firebase.GetAllUsersAsync();

            var workers = allUsers
                .Where(u =>
                    u.Role == "Worker" &&
                    u.AccountStatus == "Approved" &&
                    workerKeys.Contains(u.UniqueKey))
                .ToList();

            if (workers.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Workers",
                    "Assign workers to this project first.",
                    "OK");
                return;
            }

            var names = workers
                .Select(w => w.FullName).ToArray();

            var selected =
                await Shell.Current.DisplayActionSheet(
                    $"Pre-assign {tool.ToolName} ({tool.ToolId}) to:",
                    "Cancel", null,
                    names);

            if (selected == null ||
                selected == "Cancel") return;

            var worker = workers.FirstOrDefault(
                w => w.FullName == selected);

            if (worker is null) return;

            await _firebase.PreAssignToolAsync(
                    tool.ToolId,
                    tool.ToolName,
                    worker.UniqueKey,
                    worker.FullName,
                    ProjectId,
                    Project?.ProjectName ?? string.Empty,
                    _auth.CurrentUser?.FullName ?? "Project Engineer");

            await Shell.Current.DisplayAlert(
                "✅ Pre-assigned",
                $"{tool.ToolName} ({tool.ToolId}) " +
                $"pre-assigned to {worker.FullName}.\n\n" +
                $"Worker will confirm receipt when " +
                $"they arrive at the site.",
                "OK");

            await LoadAsync();
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

                // Load deployed tools
                var toolIds = await _firebase
                    .GetProjectToolIdsAsync(ProjectId);

                var allTools =
                    await _firebase.GetAllToolsAsync();

                _allDeployedTools.Clear();
                foreach (var toolId in toolIds)
                {
                    var tool = allTools.FirstOrDefault(
                        t => t.ToolId == toolId);
                    if (tool != null)
                        _allDeployedTools.Add(tool);
                }

                BorrowedCount = _allDeployedTools
                    .Count(t => t.Status == "Borrowed");

                _toolsPage = 1;
                ApplyToolsFilterAndPage();
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

        // ── Tools: apply search filter + page slice ─────────────────
        private void ApplyToolsFilterAndPage()
        {
            var filtered = string.IsNullOrWhiteSpace(_toolSearchText)
                ? _allDeployedTools
                : _allDeployedTools
                    .Where(t =>
                        t.ToolName.Contains(_toolSearchText, StringComparison.OrdinalIgnoreCase) ||
                        t.ToolId.Contains(_toolSearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            ToolsTotalPages = filtered.Count == 0 ? 1
                : (int)Math.Ceiling(filtered.Count / (double)ToolsPageSize);

            if (_toolsPage < 1) _toolsPage = 1;
            if (_toolsPage > ToolsTotalPages) _toolsPage = ToolsTotalPages;

            var slice = filtered
                .Skip((_toolsPage - 1) * ToolsPageSize)
                .Take(ToolsPageSize)
                .ToList();

            DeployedTools.Clear();
            foreach (var t in slice)
                DeployedTools.Add(t);

            ToolsPage = _toolsPage;

            HasTools = _allDeployedTools.Count > 0;
            ToolCount = _allDeployedTools.Count;
        }

        // ── Assign Worker ─────────────────────────────────────────
        private async Task AssignWorkerAsync()
        {
            try
            {
                var allUsers =
                    await _firebase.GetAllUsersAsync();

                // Get workers not yet assigned
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

            // Check if worker has borrowed tools
            var workerTools = _allDeployedTools
                .Where(t => t.AssignedWorkerId ==
                            worker.UniqueKey)
                .ToList();

            if (workerTools.Count > 0)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Remove Worker",
                    $"{worker.FullName} still has " +
                    $"{workerTools.Count} tool(s) assigned.\n\n" +
                    $"Transfer or return all tools first.",
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

        // ── Deploy Tool ───────────────────────────────────────────
        private async Task DeployToolAsync()
        {
            try
            {
                var allTools =
                    await _firebase.GetAllToolsAsync();

                // Get tools not yet deployed
                var deployedIds = _allDeployedTools
                    .Select(t => t.ToolId).ToList();

                var available = allTools
                    .Where(t =>
                        !deployedIds.Contains(t.ToolId) &&
                        t.Status == "Available")
                    .OrderBy(t => t.ToolName)
                    .ToList();

                if (available.Count == 0)
                {
                    await Shell.Current.DisplayAlert(
                        "No Tools Available",
                        "All available tools are already " +
                        "deployed to this project or " +
                        "are currently in use.",
                        "OK");
                    return;
                }

                var toolNames = available
                    .Select(t =>
                        $"{t.ToolName} ({t.ToolId})")
                    .ToArray();

                var selected =
                    await Shell.Current.DisplayActionSheet(
                        "Deploy Tool to Project",
                        "Cancel", null,
                        toolNames);

                if (selected == null ||
                    selected == "Cancel") return;

                var tool = available.FirstOrDefault(t =>
                    $"{t.ToolName} ({t.ToolId})" == selected);

                if (tool is null) return;

                // Add tool to project
                await _firebase.DeployToolToProjectAsync(
                    ProjectId, tool.ToolId);

                // Update tool with project ID
                tool.ProjectId = ProjectId;
                await _firebase.UpdateToolAsync(tool);

                await Shell.Current.DisplayAlert(
                    "✅ Tool Deployed",
                    $"{tool.ToolName} ({tool.ToolId}) " +
                    $"has been deployed to " +
                    $"{Project?.ProjectName}.",
                    "OK");

                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not deploy tool.\n{ex.Message}",
                    "OK");
            }
        }

        // ── Remove Tool ───────────────────────────────────────────
        private async Task RemoveToolAsync(Tool tool)
        {
            if (tool is null) return;

            if (tool.Status == "Borrowed")
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Remove Tool",
                    $"{tool.ToolName} is currently borrowed " +
                    $"by {tool.AssignedWorkerName}.\n\n" +
                    $"Tool must be available before " +
                    $"removing from project.",
                    "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Remove Tool",
                $"Remove {tool.ToolName} ({tool.ToolId}) " +
                $"from {Project?.ProjectName}?",
                "Remove", "Cancel");

            if (!confirm) return;

            try
            {
                await _firebase.RemoveToolFromProjectAsync(
                    ProjectId, tool.ToolId);

                // Remove project ID from tool
                tool.ProjectId = string.Empty;
                await _firebase.UpdateToolAsync(tool);

                await Shell.Current.DisplayAlert(
                    "Tool Removed",
                    $"{tool.ToolName} removed from project.",
                    "OK");

                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not remove tool.\n{ex.Message}",
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
    }
}