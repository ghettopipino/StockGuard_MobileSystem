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

        // ─────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────

        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        // ─────────────────────────────────────────────────────────
        // ACTIVE PROJECT
        // ─────────────────────────────────────────────────────────

        private Project? _activeProject;

        public Project? ActiveProject
        {
            get => _activeProject;
            private set
            {
                SetProperty(ref _activeProject, value);

                OnPropertyChanged(
                    nameof(HasActiveProject));

                OnPropertyChanged(
                    nameof(NoActiveProject));

                OnPropertyChanged(
                    nameof(ActiveProjectName));

                OnPropertyChanged(
                    nameof(ActiveProjectLocation));
            }
        }

        public bool HasActiveProject =>
            ActiveProject != null;

        public bool NoActiveProject =>
            ActiveProject == null;

        public string ActiveProjectName =>
            ActiveProject?.ProjectName ??
            "No Active Project";

        public string ActiveProjectLocation =>
            ActiveProject?.Location ??
            string.Empty;

        // ─────────────────────────────────────────────────────────
        // PROJECTS
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<Project>
            Projects
        { get; } = new();

        // ─────────────────────────────────────────────────────────
        // STATS
        // ─────────────────────────────────────────────────────────

        private int _totalProjects;

        public int TotalProjects
        {
            get => _totalProjects;
            private set =>
                SetProperty(
                    ref _totalProjects,
                    value);
        }

        private int _activeCount;

        public int ActiveCount
        {
            get => _activeCount;
            private set =>
                SetProperty(
                    ref _activeCount,
                    value);
        }

        private int _completedCount;

        public int CompletedCount
        {
            get => _completedCount;
            private set =>
                SetProperty(
                    ref _completedCount,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // EMPTY STATE
        // ─────────────────────────────────────────────────────────

        private bool _hasProjects;

        public bool HasProjects
        {
            get => _hasProjects;
            private set
            {
                SetProperty(ref _hasProjects, value);

                OnPropertyChanged(
                    nameof(NoProjects));
            }
        }

        public bool NoProjects =>
            !HasProjects;

        // ─────────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────────

        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set =>
                SetProperty(
                    ref _isRefreshing,
                    value);
        }

        // ─────────────────────────────────────────────────────────
        // COMMANDS
        // ─────────────────────────────────────────────────────────

        public ICommand GoBackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public ICommand CreateProjectCommand { get; }
        public ICommand ViewProjectCommand { get; }
        public ICommand SetActiveCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ScanQrCommand { get; }

        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

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
                MainThread.BeginInvokeOnMainThread(
                    () =>
                        OnPropertyChanged(
                            nameof(ThemeIcon)));

            GoBackCommand =
                new Command(
                    async () =>
                        await Shell.Current
                            .GoToAsync(".."));

            RefreshCommand =
                new Command(
                    async () =>
                        await RefreshAsync());

            ToggleThemeCommand =
                new Command(
                    () => _theme.Toggle());

            CreateProjectCommand =
                new Command(
                    async () =>
                        await CreateProjectAsync());

            ViewProjectCommand =
                new Command<Project>(
                    async project =>
                        await ViewProjectAsync(
                            project));

            SetActiveCommand =
                new Command<Project>(
                    async project =>
                        await SetActiveAsync(
                            project));

            CompleteCommand =
                new Command<Project>(
                    async project =>
                        await CompleteProjectAsync(
                            project));

            DeleteCommand =
                new Command<Project>(
                    async project =>
                        await DeleteProjectAsync(
                            project));

            ScanQrCommand =
                new Command(
                    async () =>
                        await ScanQrAsync());

            MainThread.BeginInvokeOnMainThread(
                async () =>
                    await LoadProjectsAsync());
        }

        // ─────────────────────────────────────────────────────────
        // LOAD PROJECTS
        // ─────────────────────────────────────────────────────────

        public async Task LoadProjectsAsync()
        {
            IsBusy = true;

            try
            {
                var user =
                    _auth.CurrentUser;

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

                var allProjects =
                    await _firebase
                        .GetAllProjectsAsync();

                // A PE manages only projects
                // they created.
                var projects =
                    allProjects
                        .Where(p =>
                            !p.IsDeleted &&
                            p.CreatedBy ==
                            user.UniqueKey)
                        .OrderByDescending(p =>
                            p.StartDate)
                        .ToList();

                TotalProjects =
                    projects.Count;

                ActiveCount =
                    projects.Count(p =>
                        p.Status == "Active");

                CompletedCount =
                    projects.Count(p =>
                        p.Status == "Completed");

                ActiveProject =
                    projects.FirstOrDefault(p =>
                        p.Status == "Active");

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

        // ─────────────────────────────────────────────────────────
        // REFRESH
        // ─────────────────────────────────────────────────────────

        private async Task RefreshAsync()
        {
            IsRefreshing = true;

            try
            {
                await LoadProjectsAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // CREATE PROJECT
        // ─────────────────────────────────────────────────────────

        private async Task CreateProjectAsync()
        {
            var name =
                await Shell.Current
                    .DisplayPromptAsync(
                        "New Project",
                        "Enter project name:",
                        "Next",
                        "Cancel",
                        placeholder:
                            "e.g. SM Mall Construction");

            if (string.IsNullOrWhiteSpace(name))
                return;

            var location =
                await Shell.Current
                    .DisplayPromptAsync(
                        "Project Location",
                        "Enter project location:",
                        "Next",
                        "Cancel",
                        placeholder:
                            "e.g. Cebu City");

            if (string.IsNullOrWhiteSpace(location))
                return;

            var description =
                await Shell.Current
                    .DisplayPromptAsync(
                        "Project Description",
                        "Brief description (optional):",
                        "Create",
                        "Skip",
                        placeholder:
                            "e.g. Commercial building construction");

            IsBusy = true;

            try
            {
                var user =
                    _auth.CurrentUser;

                if (user == null)
                    return;

                var projectId =
                    $"PRJ-{DateTime.Now:yyyyMMddHHmmss}";

                var project =
                    new Project
                    {
                        ProjectId =
                            projectId,

                        ProjectName =
                            name.Trim(),

                        Location =
                            location.Trim(),

                        Description =
                            description?.Trim()
                            ?? string.Empty,

                        StartDate =
                            DateTime.Now,

                        Status =
                            "Active",

                        CreatedBy =
                            user.UniqueKey,

                        CreatedByName =
                            user.FullName,

                        IsDeleted =
                            false
                    };

                /*
                 * Manuscript rule:
                 * only one project is active at a time.
                 *
                 * Keep this GLOBAL, not only for this PE.
                 */
                var existing =
                    await _firebase
                        .GetAllProjectsAsync();

                foreach (var active in
                    existing.Where(p =>
                        p.Status == "Active"))
                {
                    active.Status =
                        "Paused";

                    await _firebase
                        .UpdateProjectAsync(active);
                }

                var created =
                    await _firebase
                        .CreateProjectAsync(project);

                if (!created)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not create project.",
                        "OK");

                    return;
                }

                await Shell.Current.DisplayAlert(
                    "Project Created",
                    $"{project.ProjectName} has been created " +
                    $"and set as the active project.",
                    "OK");

                await LoadProjectsAsync();

                await Shell.Current.GoToAsync(
                    $"{nameof(ProjectDetailsView)}" +
                    $"?projectId=" +
                    $"{Uri.EscapeDataString(projectId)}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not create project.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // VIEW PROJECT
        // ─────────────────────────────────────────────────────────

        private async Task ViewProjectAsync(
            Project project)
        {
            if (project is null)
                return;

            await Shell.Current.GoToAsync(
                $"{nameof(ProjectDetailsView)}" +
                $"?projectId=" +
                $"{Uri.EscapeDataString(project.ProjectId)}");
        }

        // ─────────────────────────────────────────────────────────
        // SET ACTIVE
        // ─────────────────────────────────────────────────────────

        private async Task SetActiveAsync(
            Project project)
        {
            if (project is null ||
                project.Status == "Active" ||
                project.Status == "Completed")
            {
                return;
            }

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Set Active Project",
                    $"Switch to {project.ProjectName}?\n\n" +
                    "The currently active project will be paused.",
                    "Switch",
                    "Cancel");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                var success =
                    await _firebase
                        .SetActiveProjectAsync(
                            project.ProjectId);

                if (!success)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not switch the active project.",
                        "OK");

                    return;
                }

                await Shell.Current.DisplayAlert(
                    "Project Switched",
                    $"{project.ProjectName} is now the active project.",
                    "OK");

                await LoadProjectsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not switch project.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // COMPLETE PROJECT
        // ─────────────────────────────────────────────────────────

        private async Task CompleteProjectAsync(
            Project project)
        {
            if (project is null ||
                project.Status == "Completed")
            {
                return;
            }

            IsBusy = true;

            try
            {
                var allTools =
                    await _firebase
                        .GetAllToolsAsync(
                            forceRefresh: true);

                /*
                 * A project cannot be completed while
                 * workers still physically hold equipment.
                 *
                 * End-Day Check-In does NOT affect this.
                 * Checked-in equipment is still Borrowed.
                 */
                var outstandingTools =
                    allTools
                        .Where(t =>
                            t.BorrowedProjectId ==
                                project.ProjectId &&
                            (
                                t.Status == "Borrowed" ||
                                t.Status == "PendingReturn"
                            ))
                        .ToList();

                if (outstandingTools.Count > 0)
                {
                    int borrowedCount =
                        outstandingTools.Count(t =>
                            t.Status == "Borrowed");

                    int pendingReturnCount =
                        outstandingTools.Count(t =>
                            t.Status == "PendingReturn");

                    await Shell.Current.DisplayAlert(
                        "Cannot Complete Project",
                        $"{project.ProjectName} still has equipment " +
                        $"that has not been fully returned.\n\n" +
                        $"Borrowed: {borrowedCount}\n" +
                        $"Pending Return: {pendingReturnCount}\n\n" +
                        "Receive and verify all equipment before " +
                        "completing the project.",
                        "OK");

                    return;
                }

                bool confirm =
                    await Shell.Current.DisplayAlert(
                        "Complete Project",
                        $"Mark {project.ProjectName} as completed?\n\n" +
                        "All assigned equipment has been returned.",
                        "Complete",
                        "Cancel");

                if (!confirm)
                    return;

                project.Status =
                    "Completed";

                project.EndDate =
                    DateTime.Now;

                var updated =
                    await _firebase
                        .UpdateProjectAsync(
                            project);

                if (!updated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not complete the project.",
                        "OK");

                    return;
                }

                await Shell.Current.DisplayAlert(
                    "Project Completed",
                    $"{project.ProjectName} has been marked as completed.",
                    "OK");

                await LoadProjectsAsync();

                await Shell.Current.GoToAsync(
                    $"{nameof(ProjectAnalyticsView)}" +
                    $"?projectId=" +
                    $"{Uri.EscapeDataString(project.ProjectId)}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not complete project.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // DELETE PROJECT
        // ─────────────────────────────────────────────────────────

        private async Task DeleteProjectAsync(
            Project project)
        {
            if (project is null)
                return;

            if (project.Status == "Active")
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Delete",
                    "An active project cannot be deleted.\n\n" +
                    "Complete the project first.",
                    "OK");

                return;
            }

            bool confirm =
                await Shell.Current.DisplayAlert(
                    "Delete Project",
                    $"Delete {project.ProjectName}?\n\n" +
                    "This cannot be undone.",
                    "Delete",
                    "Cancel");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                project.IsDeleted =
                    true;

                var updated =
                    await _firebase
                        .UpdateProjectAsync(
                            project);

                if (!updated)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "Could not delete project.",
                        "OK");

                    return;
                }

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
                    $"Could not delete project.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // QR SCANNER
        // ─────────────────────────────────────────────────────────

        private async Task ScanQrAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(QrScannerView)}" +
                    $"?mode=AssignEquipment");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    ex.Message,
                    "OK");
            }
        }
    }
}