using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    [QueryProperty(nameof(ProjectId), "projectId")]
    [QueryProperty(nameof(SelectMode), "selectMode")]
    public class BulkSelectViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebase;
        private readonly ThemeService _theme;

        // ── Query Properties ──────────────────────────────────────
        private string _projectId = string.Empty;
        public string ProjectId
        {
            get => _projectId;
            set
            {
                SetProperty(ref _projectId, value);
                if (!string.IsNullOrEmpty(value) &&
                    !string.IsNullOrEmpty(SelectMode))
                    MainThread.BeginInvokeOnMainThread(
                        async () => await LoadAsync());
            }
        }

        // "workers" | "tools"
        private string _selectMode = string.Empty;
        public string SelectMode
        {
            get => _selectMode;
            set
            {
                SetProperty(ref _selectMode, value);
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(PageSubtitle));
                OnPropertyChanged(nameof(IsWorkerMode));
                OnPropertyChanged(nameof(IsToolMode));
                if (!string.IsNullOrEmpty(value) &&
                    !string.IsNullOrEmpty(ProjectId))
                    MainThread.BeginInvokeOnMainThread(
                        async () => await LoadAsync());
            }
        }

        public string PageTitle => IsWorkerMode
            ? "Assign Workers"
            : "Deploy Tools";

        public string PageSubtitle => IsWorkerMode
            ? "Select workers to assign to project"
            : "Select tools to deploy to project";

        public bool IsWorkerMode =>
            SelectMode == "workers";

        public bool IsToolMode =>
            SelectMode == "tools";

        // ── Theme ─────────────────────────────────────────────────
        public string ThemeIcon =>
            _theme.IsDark ? "🌙" : "☀️";

        // ── Collections ───────────────────────────────────────────
        public ObservableCollection<SelectableItem>
            Items
        { get; } = new();

        // ── Stats ─────────────────────────────────────────────────
        private int _selectedCount;
        public int SelectedCount
        {
            get => _selectedCount;
            set
            {
                SetProperty(ref _selectedCount, value);
                OnPropertyChanged(nameof(SelectedLabel));
                OnPropertyChanged(nameof(HasSelection));
            }
        }

        public string SelectedLabel =>
            SelectedCount == 0
                ? "None selected"
                : $"{SelectedCount} selected";

        public bool HasSelection => SelectedCount > 0;

        // ── Empty State ───────────────────────────────────────────
        private bool _hasItems;
        public bool HasItems
        {
            get => _hasItems;
            private set
            {
                SetProperty(ref _hasItems, value);
                OnPropertyChanged(nameof(NoItems));
            }
        }
        public bool NoItems => !HasItems;

        public string EmptyMessage => IsWorkerMode
            ? "All approved workers are already " +
              "assigned to this project"
            : "All available tools are already " +
              "deployed to this project";

        // ── Commands ──────────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand ConfirmCommand { get; }
        public ICommand ToggleItemCommand { get; }

        // ── Constructor ───────────────────────────────────────────
        public BulkSelectViewModel(
            FirebaseService firebase,
            ThemeService theme)
        {
            _firebase = firebase;
            _theme = theme;

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            GoBackCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            ToggleThemeCommand =
                new Command(() => _theme.Toggle());

            SelectAllCommand = new Command(() =>
            {
                foreach (var item in Items)
                    item.IsSelected = true;
                UpdateSelectedCount();
            });

            ClearAllCommand = new Command(() =>
            {
                foreach (var item in Items)
                    item.IsSelected = false;
                UpdateSelectedCount();
            });

            ConfirmCommand = new Command(
                async () => await ConfirmSelectionAsync(),
                () => HasSelection);

            ToggleItemCommand =
                new Command<SelectableItem>(item =>
                {
                    if (item is null) return;
                    item.IsSelected = !item.IsSelected;
                    UpdateSelectedCount();
                });
        }

        // ── Load Items ────────────────────────────────────────────
        public async Task LoadAsync()
        {
            if (string.IsNullOrEmpty(ProjectId) ||
                string.IsNullOrEmpty(SelectMode)) return;

            IsBusy = true;
            try
            {
                Items.Clear();

                if (IsWorkerMode)
                    await LoadWorkersAsync();
                else
                    await LoadToolsAsync();

                HasItems = Items.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"BulkSelect load error: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        private async Task LoadWorkersAsync()
        {
            var allUsers =
                await _firebase.GetAllUsersAsync();

            // ✅ Get fresh list from Firebase every time
            var assignedKeys = await _firebase
                .GetProjectWorkerKeysAsync(ProjectId);

            // Show only workers NOT yet assigned
            var available = allUsers
                .Where(u =>
                    u.Role == "Worker" &&
                    u.AccountStatus == "Approved" &&
                    !assignedKeys.Contains(u.UniqueKey))
                .OrderBy(u => u.FullName)
                .ToList();

            // ✅ Clear before adding
            Items.Clear();

            foreach (var worker in available)
            {
                Items.Add(new SelectableItem
                {
                    Id = worker.UniqueKey,
                    Name = worker.FullName,
                    SubTitle = worker.Email,
                    Icon = "👷",
                    IsSelected = false
                });
            }
        }

        private async Task LoadToolsAsync()
        {
            var allTools =
                await _firebase.GetAllToolsAsync();

            // ✅ Get fresh list from Firebase every time
            var deployedIds = await _firebase
                .GetProjectToolIdsAsync(ProjectId);

            // Show only available tools not yet deployed
            var available = allTools
                .Where(t =>
                    !deployedIds.Contains(t.ToolId) &&
                    t.Status == "Available")
                .OrderBy(t => t.ToolName)
                .ThenBy(t => t.ToolId)
                .ToList();

            // ✅ Clear before adding
            Items.Clear();

            foreach (var tool in available)
            {
                Items.Add(new SelectableItem
                {
                    Id = tool.ToolId,
                    Name = tool.ToolName,
                    SubTitle = tool.ToolId,
                    Icon = tool.ToolIcon,
                    IsSelected = false
                });
            }
        }

        private void UpdateSelectedCount()
        {
            SelectedCount = Items.Count(i => i.IsSelected);
            ((Command)ConfirmCommand)
                .ChangeCanExecute();
        }

        // ── Confirm Selection ─────────────────────────────────────
        private async Task ConfirmSelectionAsync()
        {
            var selected = Items
                .Where(i => i.IsSelected)
                .ToList();

            if (selected.Count == 0) return;

            IsBusy = true;
            try
            {
                if (IsWorkerMode)
                {
                    // ✅ Get already assigned workers first
                    var alreadyAssigned = await _firebase
                        .GetProjectWorkerKeysAsync(ProjectId);

                    foreach (var item in selected)
                    {
                        // ✅ Skip if already assigned
                        if (alreadyAssigned.Contains(item.Id))
                            continue;

                        await _firebase
                            .AssignWorkerToProjectAsync(
                                ProjectId, item.Id);
                    }

                    await Shell.Current.DisplayAlert(
                        "✅ Workers Assigned",
                        $"{selected.Count} worker(s) " +
                        $"assigned to project successfully.",
                        "OK");
                }
                else
                {
                    // ✅ Get already deployed tools first
                    var alreadyDeployed = await _firebase
                        .GetProjectToolIdsAsync(ProjectId);

                    foreach (var item in selected)
                    {
                        // ✅ Skip if already deployed
                        if (alreadyDeployed.Contains(item.Id))
                            continue;

                        await _firebase
                            .DeployToolToProjectAsync(
                                ProjectId, item.Id);

                       
                    }

                    await Shell.Current.DisplayAlert(
                        "✅ Tools Deployed",
                        $"{selected.Count} tool(s) " +
                        $"deployed to project successfully.",
                        "OK");
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not complete selection.\n" +
                    $"{ex.Message}",
                    "OK");
            }
            finally { IsBusy = false; }
        }
    }
}