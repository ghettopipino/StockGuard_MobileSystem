using System.Collections.ObjectModel;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    public class WorkerDashboardViewModel : BaseViewModel
    {
        private readonly AuthService _auth;
        private readonly ThemeService _theme;
        private readonly FirebaseService _firebase;

        // ── Identity ──────────────────────────────────────────────
        public string WorkerName =>
            _auth.CurrentUser?.FullName ?? "Worker";

        public string WorkerFirstName =>
            ((_auth.CurrentUser?.FullName) ?? "Worker")
            .Split(' ')[0];

        public string WorkerInitials =>
            GetInitials(_auth.CurrentUser?.FullName);

        public string GreetingText =>
            GetGreeting() + ",";

        public string TodayDate =>
            DateTime.Now.ToString("dddd, MMMM d, yyyy");

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
                OnPropertyChanged(nameof(ActiveProjectDuration));
            }
        }

        public bool HasActiveProject => ActiveProject != null;
        public bool NoActiveProject => ActiveProject == null;

        public string ActiveProjectName =>
            ActiveProject?.ProjectName ?? "No Active Project";

        public string ActiveProjectLocation =>
            ActiveProject?.Location ?? string.Empty;

        public string ActiveProjectDuration =>
            ActiveProject?.StartDateLabel ?? string.Empty;

        // ── Deployment Status ─────────────────────────────────────
        private bool _isDeployed;
        public bool IsDeployed
        {
            get => _isDeployed;
            private set
            {
                SetProperty(ref _isDeployed, value);
                OnPropertyChanged(nameof(IsNotDeployed));
                OnPropertyChanged(nameof(DeploymentStatusText));
                OnPropertyChanged(nameof(DeploymentStatusColor));
                OnPropertyChanged(nameof(DeploymentStatusIcon));
            }
        }

        public bool IsNotDeployed => !IsDeployed;

        public string DeploymentStatusText =>
            IsDeployed
                ? "You are deployed to this project"
                : "You are not deployed to this project";

        public string DeploymentStatusColor =>
            IsDeployed ? "#10b981" : "#f59e0b";

        public string DeploymentStatusIcon =>
            IsDeployed ? "✅" : "⚠️";

        // ── Stats ─────────────────────────────────────────────────
        private int _assignedCount;
        public int AssignedCount
        {
            get => _assignedCount;
            private set
            {
                SetProperty(ref _assignedCount, value);
                HasNoTools = value == 0;
                OnPropertyChanged(nameof(HasPendingRequests));
            }
        }

        private int _pendingCount;
        public int PendingCount
        {
            get => _pendingCount;
            private set
            {
                SetProperty(ref _pendingCount, value);
                OnPropertyChanged(nameof(HasPendingRequests));
            }
        }

        private int _returnedTodayCount;
        public int ReturnedTodayCount
        {
            get => _returnedTodayCount;
            private set => SetProperty(ref _returnedTodayCount, value);
        }

        private bool _hasNoTools;
        public bool HasNoTools
        {
            get => _hasNoTools;
            private set => SetProperty(ref _hasNoTools, value);
        }

        public bool HasPendingRequests => PendingCount > 0;
        public bool HasNoAssignedTools => AssignedCount == 0;

        // ═══════════════════════════════════════════════════════════
        //  MY TOOLS — PAGINATION
        //  Page size = 3 tools per page.
        // ═══════════════════════════════════════════════════════════
        private const int ToolsPageSize = 3;
        private readonly List<ToolAssignmentItem> _allTools = new();

        public ObservableCollection<ToolAssignmentItem>
            AssignedTools
        { get; } = new();

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
        // Only show pager row if there is more than one page
        public bool ShowToolsPager => ToolsTotalPages > 1;

        // ═══════════════════════════════════════════════════════════
        //  RECENT ACTIVITY — FILTER + PAGINATION
        //  Filters: All | Borrowed | Returned | Transferred |
        //           Damaged | Paused | Resumed | Request
        //  Page size = 5 items per page.
        //  Items are grouped by DayLabel in XAML.
        // ═══════════════════════════════════════════════════════════
        private const int ActivityPageSize = 5;
        private readonly List<ActivityItem> _allActivity = new();

        public ObservableCollection<ActivityItem>
            RecentActivity
        { get; } = new();

        private string _activityFilter = "All";
        public string ActivityFilter
        {
            get => _activityFilter;
            set
            {
                SetProperty(ref _activityFilter, value);
                _activityPage = 1;
                ApplyActivityPage();
            }
        }

        public ObservableCollection<PendingAssignmentItem>
    PendingAssignments
        { get; } = new();

        private bool _hasPendingAssignments;
        public bool HasPendingAssignments
        {
            get => _hasPendingAssignments;
            private set => SetProperty(ref _hasPendingAssignments, value);
        }

        private int _activityPage = 1;
        public int ActivityPage
        {
            get => _activityPage;
            private set
            {
                SetProperty(ref _activityPage, value);
                OnPropertyChanged(nameof(ActivityPageLabel));
                OnPropertyChanged(nameof(CanGoActivityPrev));
                OnPropertyChanged(nameof(CanGoActivityNext));
                OnPropertyChanged(nameof(ShowActivityPager));
            }
        }

        private int _activityTotalPages = 1;
        public int ActivityTotalPages
        {
            get => _activityTotalPages;
            private set
            {
                SetProperty(ref _activityTotalPages, value);
                OnPropertyChanged(nameof(ActivityPageLabel));
                OnPropertyChanged(nameof(CanGoActivityPrev));
                OnPropertyChanged(nameof(CanGoActivityNext));
                OnPropertyChanged(nameof(ShowActivityPager));
            }
        }

        public string ActivityPageLabel => $"{ActivityPage} / {ActivityTotalPages}";
        public bool CanGoActivityPrev => ActivityPage > 1;
        public bool CanGoActivityNext => ActivityPage < ActivityTotalPages;
        public bool ShowActivityPager => ActivityTotalPages > 1;

        // ── Pull to Refresh ───────────────────────────────────────
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // ── Commands ──────────────────────────────────────────────
        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ScanQrCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewToolCommand { get; }
        public ICommand ToolsPrevCommand { get; }
        public ICommand ToolsNextCommand { get; }
        public ICommand SetActivityFilterCommand { get; }
        public ICommand ActivityPrevCommand { get; }
        public ICommand ActivityNextCommand { get; }
        public ICommand AcceptAssignmentCommand { get; }
        public ICommand DeclineAssignmentCommand { get; }


        // ── Constructor ───────────────────────────────────────────
        public WorkerDashboardViewModel(
            AuthService auth,
            ThemeService theme,
            FirebaseService firebase)
        {
            _auth = auth;
            _theme = theme;
            _firebase = firebase;
            Title = "Dashboard";

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            ToggleThemeCommand = new Command(() => _theme.Toggle());
            LogoutCommand = new Command(async () => await LogoutAsync());
            ScanQrCommand = new Command(async () => await ScanQrAsync());
            RefreshCommand = new Command(async () => await RefreshAsync());

            LogoutCommand = new Command(async () =>
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Logout", "Are you sure you want to logout?", "Logout", "Cancel");
                if (!confirm) return;

                _auth.Logout(); // or however your AuthService handles sign-out
                await Shell.Current.GoToAsync("//LoginView");
            });

            ViewToolCommand = new Command<ToolAssignmentItem>(
                async item => await ViewToolAsync(item));

            ToolsPrevCommand = new Command(() =>
            {
                if (!CanGoToolsPrev) return;
                _toolsPage--;
                ApplyToolsPage();
            });

            ToolsNextCommand = new Command(() =>
            {
                if (!CanGoToolsNext) return;
                _toolsPage++;
                ApplyToolsPage();
            });

            SetActivityFilterCommand = new Command<string>(filter =>
            {
                ActivityFilter = filter ?? "All";
            });

            ActivityPrevCommand = new Command(() =>
            {
                if (!CanGoActivityPrev) return;
                _activityPage--;
                ApplyActivityPage();
            });

            ActivityNextCommand = new Command(() =>
            {
                if (!CanGoActivityNext) return;
                _activityPage++;
                ApplyActivityPage();
            });

            AcceptAssignmentCommand = new Command<PendingAssignmentItem>(
                async item => await AcceptAssignmentAsync(item));

            DeclineAssignmentCommand = new Command<PendingAssignmentItem>(
                async item => await DeclineAssignmentAsync(item));

            MainThread.BeginInvokeOnMainThread(
                async () => await LoadDashboardDataAsync());

        }
        

        // ── View Tool ─────────────────────────────────────────────
        private async Task ViewToolAsync(ToolAssignmentItem item)
        {
            if (item is null) return;
            await Shell.Current.GoToAsync(
                $"{nameof(WorkerToolDetailsView)}" +
                $"?toolId={Uri.EscapeDataString(item.ToolId)}");
        }

        // ── Apply tools page slice ────────────────────────────────
        private void ApplyToolsPage()
        {
            ToolsTotalPages = _allTools.Count == 0 ? 1
                : (int)Math.Ceiling(_allTools.Count / (double)ToolsPageSize);

            if (_toolsPage < 1) _toolsPage = 1;
            if (_toolsPage > ToolsTotalPages) _toolsPage = ToolsTotalPages;

            var slice = _allTools
                .Skip((_toolsPage - 1) * ToolsPageSize)
                .Take(ToolsPageSize)
                .ToList();

            AssignedTools.Clear();
            foreach (var t in slice)
                AssignedTools.Add(t);

            ToolsPage = _toolsPage;
        }

        // ── Apply activity filter + page slice ────────────────────
        private void ApplyActivityPage()
        {
            var filtered = _activityFilter == "All"
                ? _allActivity
                : _allActivity
                    .Where(a => a.ActivityType
                        .Equals(_activityFilter,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            ActivityTotalPages = filtered.Count == 0 ? 1
                : (int)Math.Ceiling(filtered.Count / (double)ActivityPageSize);

            if (_activityPage < 1) _activityPage = 1;
            if (_activityPage > ActivityTotalPages)
                _activityPage = ActivityTotalPages;

            var slice = filtered
                .Skip((_activityPage - 1) * ActivityPageSize)
                .Take(ActivityPageSize)
                .ToList();

            RecentActivity.Clear();

            if (slice.Count == 0)
            {
                RecentActivity.Add(new ActivityItem
                {
                    Icon = "📋",
                    Description = "No activity found",
                    TimeAgo = "",
                    ActivityType = "None",
                    Date = DateTime.MinValue
                });
            }
            else
            {
                foreach (var a in slice)
                    RecentActivity.Add(a);
            }

            ActivityPage = _activityPage;
        }

        // ── Load Dashboard Data ───────────────────────────────────
        private async Task LoadDashboardDataAsync()
        {
            IsBusy = true;
            try
            {
                var currentUser = _auth.CurrentUser;
                if (currentUser is null) return;

                var workerId = currentUser.UniqueKey;

                // ── Parallel fetch ─────────────────────────────────
                var projectTask = _firebase.GetActiveProjectAsync();
                var toolsTask = _firebase.GetToolsByWorkerAsync(workerId);
                var requestsTask = _firebase.GetAllBorrowRequestsRawAsync();
                var transfersTask = _firebase.GetAllTransferRequestsRawAsync();
                var transactionsTask = _firebase.GetWorkerTransactionsAsync(workerId);
                var pendingAssignTask = _firebase.GetPendingAssignmentsForWorkerAsync(workerId);

                await Task.WhenAll(
                    projectTask,
                    toolsTask,
                    requestsTask,
                    transfersTask,
                    transactionsTask,
                     pendingAssignTask); 

                var activeProject = projectTask.Result;
                var tools = toolsTask.Result;
                var allRequests = requestsTask.Result;
                var allTransfers = transfersTask.Result;
                var transactions = transactionsTask.Result;

                // ── Active project + deployment ────────────────────
                ActiveProject = activeProject;

                if (activeProject != null)
                {
                    var workerKeys = await _firebase
                        .GetProjectWorkerKeysAsync(
                            activeProject.ProjectId);
                    IsDeployed = workerKeys.Contains(workerId);
                }
                else
                {
                    IsDeployed = false;
                }

                // ── My Tools ───────────────────────────────────────
                _allTools.Clear();
                foreach (var tool in tools)
                {
                    _allTools.Add(new ToolAssignmentItem
                    {
                        ToolId = tool.ToolId,
                        ToolName = tool.ToolName,
                        Status = tool.Status,
                        BorrowDate = tool.BorrowDate ?? DateTime.Now
                    });
                }

                AssignedCount = _allTools.Count;
                _toolsPage = 1;
                ApplyToolsPage();

                var pendingAssignments = pendingAssignTask.Result;
                PendingAssignments.Clear();
                foreach (var pa in pendingAssignments)
                {
                    PendingAssignments.Add(new PendingAssignmentItem
                    {
                        Key = pa.Key,
                        ToolId = pa.Assignment.ToolId,
                        ToolName = pa.Assignment.ToolName,
                        ProjectName = pa.Assignment.ProjectName,
                        AssignedByName = pa.Assignment.AssignedByName,
                        Assignment = pa.Assignment
                    });
                }
                HasPendingAssignments = PendingAssignments.Count > 0;

                // ── Pre-filter requests/transfers ──────────────────
                var myRequests = allRequests
                    .Where(r =>
                        r.Request.RequesterId == workerId ||
                        r.Request.OwnerId == workerId)
                    .Take(20)
                    .ToList();

                var myTransfers = allTransfers
                    .Where(t =>
                        t.Request.FromWorkerId == workerId ||
                        t.Request.ToWorkerId == workerId)
                    .Take(20)
                    .ToList();

                // ── Pending count ──────────────────────────────────
                PendingCount =
                    allRequests.Count(r =>
                        r.Request.OwnerId == workerId &&
                        r.Request.Status == "Pending") +
                    allTransfers.Count(t =>
                        t.Request.ToWorkerId == workerId &&
                        t.Request.Status == "Pending")+
                         pendingAssignments.Count; 


                // ── Returned today ─────────────────────────────────
                ReturnedTodayCount = transactions
                    .Count(t =>
                        t.Action == "Returned" &&
                        t.Date.Date == DateTime.Today);

                // ── Build full activity list ───────────────────────
                var activityItems =
                    new List<(DateTime Date, ActivityItem Item)>();

                foreach (var tx in transactions)
                {
                    activityItems.Add((tx.Date, new ActivityItem
                    {
                        Icon = tx.ActionIcon,
                        Description = tx.Description,
                        TimeAgo = tx.DateLabel,
                        ActivityType = tx.Action,
                        Date = tx.Date
                    }));
                }

                foreach (var item in myRequests)
                {
                    var req = item.Request;

                    if (req.RequesterId == workerId)
                    {
                        var icon = req.Status switch
                        {
                            "Approved" => "✅",
                            "Declined" => "❌",
                            _ => "📩"
                        };
                        var desc = req.Status switch
                        {
                            "Approved" =>
                                $"Your request for {req.ToolName} was approved",
                            "Declined" =>
                                $"Your request for {req.ToolName} was declined",
                            _ =>
                                $"You requested {req.ToolName} ({req.ToolId})"
                        };
                        activityItems.Add((req.RequestDate, new ActivityItem
                        {
                            Icon = icon,
                            Description = desc,
                            TimeAgo = GetDateLabel(req.RequestDate),
                            ActivityType = "Request",
                            Date = req.RequestDate
                        }));
                    }

                    if (req.OwnerId == workerId)
                    {
                        var icon = req.Status switch
                        {
                            "Approved" => "✅",
                            "Declined" => "❌",
                            _ => "🔔"
                        };
                        var desc = req.Status switch
                        {
                            "Approved" =>
                                $"You approved {req.RequesterName}'s request",
                            "Declined" =>
                                $"You declined {req.RequesterName}'s request",
                            _ =>
                                $"{req.RequesterName} requested {req.ToolName}"
                        };
                        activityItems.Add((req.RequestDate, new ActivityItem
                        {
                            Icon = icon,
                            Description = desc,
                            TimeAgo = GetDateLabel(req.RequestDate),
                            ActivityType = "Request",
                            Date = req.RequestDate
                        }));
                    }
                }

                foreach (var item in myTransfers)
                {
                    var req = item.Request;

                    if (req.FromWorkerId == workerId)
                    {
                        var icon = req.Status switch
                        {
                            "Accepted" => "✅",
                            "Declined" => "❌",
                            _ => "🔄"
                        };
                        activityItems.Add((req.RequestDate, new ActivityItem
                        {
                            Icon = icon,
                            Description =
                                $"Transfer of {req.ToolName} " +
                                $"to {req.ToWorkerName}: {req.Status}",
                            TimeAgo = GetDateLabel(req.RequestDate),
                            ActivityType = "Transfer",
                            Date = req.RequestDate
                        }));
                    }

                    if (req.ToWorkerId == workerId)
                    {
                        var icon = req.Status switch
                        {
                            "Accepted" => "✅",
                            "Declined" => "❌",
                            _ => "🔔"
                        };
                        activityItems.Add((req.RequestDate, new ActivityItem
                        {
                            Icon = icon,
                            Description =
                                $"{req.FromWorkerName} wants to " +
                                $"transfer {req.ToolName} to you",
                            TimeAgo = GetDateLabel(req.RequestDate),
                            ActivityType = "Transfer",
                            Date = req.RequestDate
                        }));
                    }
                }

                // Store full sorted list then apply first page
                _allActivity.Clear();
                foreach (var (_, act) in activityItems
                    .OrderByDescending(a => a.Date))
                    _allActivity.Add(act);

                _activityPage = 1;
                _activityFilter = "All";
                OnPropertyChanged(nameof(ActivityFilter));
                ApplyActivityPage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadDashboard error: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        // ── Public refresh called from OnAppearing ────────────────
        public async Task RefreshOnAppearingAsync()
        {
            try { await LoadDashboardDataAsync(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"RefreshOnAppearing error: {ex.Message}");
            }
        }

        // ── Private refresh for pull to refresh ───────────────────
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadDashboardDataAsync();
            IsRefreshing = false;
        }

        // ── Navigation ────────────────────────────────────────────
        private async Task ScanQrAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(QrScannerView));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Navigation Error",
                    $"Could not open scanner.\n\n{ex.Message}",
                    "OK");
            }
        }

        private async Task LogoutAsync()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Logout",
                "Are you sure you want to logout?",
                "Logout", "Cancel");

            if (!confirm) return;

            _auth.Logout();
            await Shell.Current.GoToAsync("//LoginView");
        }

        // ── Helpers ───────────────────────────────────────────────
        private static string GetInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "W";
            var parts = name.Trim().Split(' ',
                StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                : name[0].ToString().ToUpper();
        }

        private static string GetGreeting()
        {
            var h = DateTime.Now.Hour;
            return h < 12 ? "Good morning"
                 : h < 17 ? "Good afternoon"
                 : "Good evening";
        }

        private static string GetDateLabel(DateTime date)
        {
            var ts = DateTime.Now - date;
            if (ts.TotalMinutes < 1) return "Just now";
            if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes}m ago";
            if (ts.TotalHours < 24) return $"{(int)ts.TotalHours}h ago";
            if (ts.TotalDays < 2) return "Yesterday";
            if (ts.TotalDays < 7) return $"{(int)ts.TotalDays} days ago";
            return date.ToString("MMM d");
        }

        private async Task AcceptAssignmentAsync(PendingAssignmentItem item)
        {
            if (item?.Assignment is null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Accept Tool",
                $"Accept {item.ToolName} ({item.ToolId}) assigned by {item.AssignedByName}?",
                "Accept", "Cancel");
            if (!confirm) return;

            bool success = await _firebase.ConfirmAssignmentAsync(item.Key, item.Assignment);

            if (success)
                await Shell.Current.DisplayAlert(
                    "✅ Tool Accepted",
                    $"{item.ToolName} has been added to your tools.", "OK");
            else
                await Shell.Current.DisplayAlert(
                    "Error",
                    "This tool is no longer available.", "OK");

            await LoadDashboardDataAsync();
        }

        private async Task DeclineAssignmentAsync(PendingAssignmentItem item)
        {
            if (item?.Assignment is null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Decline Tool",
                $"Decline {item.ToolName} assigned by {item.AssignedByName}?",
                "Decline", "Cancel");
            if (!confirm) return;

            await _firebase.DeclineAssignmentAsync(item.Key, item.Assignment);
            await LoadDashboardDataAsync();
        }
    }
}