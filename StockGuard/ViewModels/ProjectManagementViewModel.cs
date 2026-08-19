using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    public class ProjectManagementViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        // ── Theme ─────────────────────────────────────────────────
        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        // ── Active Project ────────────────────────────────────────
        private Project? _activeProject;
        public Project? ActiveProject
        {
            get => _activeProject;
            private set
            {
                SetProperty(ref _activeProject, value);
                OnPropertyChanged(nameof(HasActiveProject));
                OnPropertyChanged(nameof(NoActiveProject));
                OnPropertyChanged(nameof(ActiveProjectName));
                OnPropertyChanged(nameof(ActiveProjectLocation));
            }
        }

        public bool HasActiveProject =>
            ActiveProject != null;
        public bool NoActiveProject =>
            ActiveProject == null;

        public string ActiveProjectName =>
            ActiveProject?.ProjectName ?? "No Active Project";

        public string ActiveProjectLocation =>
            ActiveProject?.Location ?? string.Empty;

        // ── Collections ───────────────────────────────────────────
        public ObservableCollection<Project>
            Projects
        { get; } = new();

        // ── Stats ─────────────────────────────────────────────────
        private int _totalProjects;
        public int TotalProjects
        {
            get => _totalProjects;
            private set => SetProperty(ref _totalProjects, value);
        }

        private int _activeCount;
        public int ActiveCount
        {
            get => _activeCount;
            private set => SetProperty(ref _activeCount, value);
        }

        private int _completedCount;
        public int CompletedCount
        {
            get => _completedCount;
            private set =>
                SetProperty(ref _completedCount, value);
        }

        // ── Empty State ───────────────────────────────────────────
        private bool _hasProjects;
        public bool HasProjects
        {
            get => _hasProjects;
            private set
            {
                SetProperty(ref _hasProjects, value);
                OnPropertyChanged(nameof(NoProjects));
            }
        }
        public bool NoProjects => !HasProjects;

        // ── Pull to Refresh ───────────────────────────────────────
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // ── Commands ──────────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand CreateProjectCommand { get; }
        public ICommand ViewProjectCommand { get; }
        public ICommand SetActiveCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ScanQrCommand { get; }


        // ── Constructor ───────────────────────────────────────────
        public ProjectManagementViewModel(
            FirebaseService firebase,
            AuthService auth,
            ThemeService theme)
        {
            _firebase = firebase;
            _auth = auth;
            _theme = theme;
            Title = "Project Management";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            GoBackCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            RefreshCommand = new Command(
                async () => await RefreshAsync());

            ToggleThemeCommand =
                new Command(() => _theme.Toggle());

            CreateProjectCommand = new Command(
                async () => await CreateProjectAsync());

            ViewProjectCommand = new Command<Project>(
                async p => await ViewProjectAsync(p));

            SetActiveCommand = new Command<Project>(
                async p => await SetActiveAsync(p));

            CompleteCommand = new Command<Project>(
                async p => await CompleteProjectAsync(p));

            DeleteCommand = new Command<Project>(
                async p => await DeleteProjectAsync(p));

            ScanQrCommand = new Command(
                async () => await ScanQrAsync());


            MainThread.BeginInvokeOnMainThread(
                async () => await LoadProjectsAsync());
        }

        // ── Load Projects ─────────────────────────────────────────
        public async Task LoadProjectsAsync()
        {
            IsBusy = true;

            try
            {
                var user = _auth.CurrentUser;

                if (user == null)
                {
                    Projects.Clear();

                    TotalProjects = 0;
                    ActiveCount = 0;
                    CompletedCount = 0;
                    ActiveProject = null;
                    HasProjects = false;

                    return;
                }

                // Get all projects from Firebase
                var allProjects =
                    await _firebase.GetAllProjectsAsync();

                // IMPORTANT:
                // A Project Engineer can only manage
                // projects that they created.
                var projects = allProjects
                    .Where(p =>
                        p.CreatedBy == user.UniqueKey)
                    .ToList();

                // ── Stats for THIS PE only ────────────────────────

                TotalProjects = projects.Count;

                ActiveCount = projects.Count(p =>
                    p.Status == "Active");

                CompletedCount = projects.Count(p =>
                    p.Status == "Completed");

                ActiveProject = projects.FirstOrDefault(p =>
                    p.Status == "Active");

                // ── Display projects ──────────────────────────────

                Projects.Clear();

                foreach (var project in projects)
                {
                    Projects.Add(project);
                }

                HasProjects =
                    Projects.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadProjects error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadProjectsAsync();
            IsRefreshing = false;
        }

        // ── Create Project ────────────────────────────────────────
        private async Task CreateProjectAsync()
        {
            // Step 1 — Project Name
            var name = await Shell.Current
                .DisplayPromptAsync(
                    "New Project",
                    "Enter project name:",
                    "Next", "Cancel",
                    placeholder:
                        "e.g. SM Mall Construction");

            if (string.IsNullOrWhiteSpace(name)) return;

            // Step 2 — Location
            var location = await Shell.Current
                .DisplayPromptAsync(
                    "Project Location",
                    "Enter project location:",
                    "Next", "Cancel",
                    placeholder: "e.g. Cebu City");

            if (string.IsNullOrWhiteSpace(location))
                return;

            // Step 3 — Description
            var description = await Shell.Current
                .DisplayPromptAsync(
                    "Project Description",
                    "Brief description (optional):",
                    "Create", "Skip",
                    placeholder:
                        "e.g. Commercial building construction");

            IsBusy = true;
            try
            {
                var user = _auth.CurrentUser!;

                // Generate unique project ID
                var projectId =
                    $"PRJ-{DateTime.Now:yyyyMMddHHmmss}";

                var project = new Project
                {
                    ProjectId = projectId,
                    ProjectName = name.Trim(),
                    Location = location.Trim(),
                    Description = description?.Trim()
                                   ?? string.Empty,
                    StartDate = DateTime.Now,
                    Status = "Active",
                    CreatedBy = user.UniqueKey,
                    CreatedByName = user.FullName,
                    IsDeleted = false
                };

                // If creating active project
                // pause any existing active projects
                var existing =
                    await _firebase.GetAllProjectsAsync();

                foreach (var p in existing.Where(p =>
                    p.CreatedBy == user.UniqueKey &&
                    p.Status == "Active"))
                {
                    p.Status = "Paused";
                    await _firebase.UpdateProjectAsync(p);
                }

                await _firebase.CreateProjectAsync(project);

                await Shell.Current.DisplayAlert(
                    "✅ Project Created",
                    $"{name} has been created and " +
                    $"set as the active project.\n\n" +
                    $"Now assign workers and deploy " +
                    $"tools to this project.",
                    "OK");

                await LoadProjectsAsync();

                // Navigate to project details
                await Shell.Current.GoToAsync(
                    $"{nameof(ProjectDetailsView)}" +
                    $"?projectId={projectId}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not create project.\n{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }

        // ── View Project Details ──────────────────────────────────
        private async Task ViewProjectAsync(Project project)
        {
            if (project is null) return;
            await Shell.Current.GoToAsync(
                $"{nameof(ProjectDetailsView)}" +
                $"?projectId={project.ProjectId}");
        }

        // ── Set Active Project ────────────────────────────────────
        private async Task SetActiveAsync(Project project)
        {
            if (project is null ||
                project.Status == "Active" ||
                project.Status == "Completed") return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Set Active Project",
                $"Switch to {project.ProjectName}?\n\n" +
                $"Current active project will be paused.",
                "Switch", "Cancel");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                await _firebase.SetActiveProjectAsync(
                    project.ProjectId);

                await Shell.Current.DisplayAlert(
                    "✅ Project Switched",
                    $"{project.ProjectName} is now " +
                    $"the active project.",
                    "OK");

                await LoadProjectsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not switch project.\n{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }

        // ── Complete Project ──────────────────────────────────────
        private async Task CompleteProjectAsync(
            Project project)
        {
            if (project is null ||
                project.Status == "Completed") return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Complete Project",
                $"Mark {project.ProjectName} as completed?\n\n" +
                $"This will:\n" +
                $"• Return all borrowed tools\n" +
                $"• Generate analytics summary\n" +
                $"• Cannot be undone",
                "Complete", "Cancel");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                // Get all tools in project
                // Get all tools in project
                var toolIds = await _firebase
                    .GetProjectToolIdsAsync(project.ProjectId);

                foreach (var toolId in toolIds)
                {
                    var tool = await _firebase
                        .GetToolByIdAsync(toolId);

                    if (tool == null)
                        continue;

                    // ── ON HOLD ─────────────────────────────────────
                    // Do NOT release the equipment.
                    // It stays under the completed project's custody
                    // until the Project Engineer explicitly releases it.
                    if (tool.Status == "OnHold")
                    {
                        await _firebase.LogTransactionAsync(
                            new TransactionLog
                            {
                                ToolId = tool.ToolId,
                                ToolName = tool.ToolName,

                                WorkerId = tool.LastBorrowerId,
                                WorkerName = tool.LastBorrowerName,

                                ProjectId = tool.HoldProjectId,
                                ProjectName = tool.HoldProjectName,

                                Action = "ProjectCompletedOnHold",

                                Description =
                                    $"Project {project.ProjectName} completed. " +
                                    $"Tool remains On Hold at {tool.HoldLocation}.",

                                Condition = tool.Condition,
                                Date = DateTime.Now
                            });

                        // IMPORTANT:
                        // Do not clear HoldProjectId
                        // Do not clear HoldProjectName
                        // Do not clear HoldLocation
                        // Do not clear LastBorrower
                        // Do not change Status

                        continue;
                    }

                    // ── BORROWED ────────────────────────────────────
                    if (tool.Status == "Borrowed")
                    {
                        await _firebase.LogTransactionAsync(
                            new TransactionLog
                            {
                                ToolId = tool.ToolId,
                                ToolName = tool.ToolName,

                                WorkerId = tool.AssignedWorkerId,
                                WorkerName = tool.AssignedWorkerName,

                                ProjectId = tool.BorrowedProjectId,
                                ProjectName = tool.BorrowedProjectName,

                                Action = "Returned",

                                Description =
                                    $"Returned at project completion: " +
                                    $"{project.ProjectName}",

                                Condition = tool.Condition,
                                Date = DateTime.Now
                            });

                        // Return normally
                        tool.Status = "Available";

                        tool.AssignedWorkerId = string.Empty;
                        tool.AssignedWorkerName = string.Empty;

                        tool.BorrowedProjectId = string.Empty;
                        tool.BorrowedProjectName = string.Empty;

                        tool.BorrowDate = null;

                        await _firebase.UpdateToolAsync(tool);
                    }
                }

                // Mark project as completed
                project.Status = "Completed";
                project.EndDate = DateTime.Now;
                await _firebase.UpdateProjectAsync(project);

                await Shell.Current.DisplayAlert(
                     "✅ Project Completed",
                     $"{project.ProjectName} has been marked as completed.\n\n" +
                     $"Borrowed tools were returned.\n" +
                     $"Equipment currently On Hold remains held until explicitly released.",
                     "OK");
                await LoadProjectsAsync();

                // Navigate to analytics
                await Shell.Current.GoToAsync(
                    $"{nameof(ProjectAnalyticsView)}" +
                    $"?projectId={project.ProjectId}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not complete project.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }

        // ── Delete Project ────────────────────────────────────────
        private async Task DeleteProjectAsync(
            Project project)
        {
            if (project is null) return;

            if (project.Status == "Active")
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Delete",
                    "Cannot delete an active project.\n\n" +
                    "Complete or pause the project first.",
                    "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Project",
                $"Delete {project.ProjectName}?\n\n" +
                $"This cannot be undone.",
                "Delete", "Cancel");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                project.IsDeleted = true;
                await _firebase.UpdateProjectAsync(project);

                await Shell.Current.DisplayAlert(
                    "Project Deleted",
                    $"{project.ProjectName} has been deleted.",
                    "OK");

                await LoadProjectsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not delete project.\n{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }

        private async Task ScanQrAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(QrScannerView)}?mode=AssignEquipment");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
