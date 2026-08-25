using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StockGuard.Services
{
    public class NotificationSyncService
    {
        private readonly FirebaseService _firebase;
        private readonly AuthService _auth;

        private CancellationTokenSource? _debounceCts;

        public NotificationSyncService(
            FirebaseService firebase,
            AuthService auth)
        {
            _firebase = firebase;
            _auth = auth;
        }

        // ─────────────────────────────────────────────────────────
        // REFRESH NOTIFICATIONS
        // ─────────────────────────────────────────────────────────

        public async Task RefreshAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "[NotificationSync] Starting refresh...");

                var currentUser =
                    _auth.CurrentUser;

                // If nobody is logged in, clear notifications.
                if (currentUser == null)
                {
                    ClearNotifications();

                    System.Diagnostics.Debug.WriteLine(
                        "[NotificationSync] No logged-in user.");

                    return;
                }

                // ─────────────────────────────────────────────
                // LOAD DATA
                // ─────────────────────────────────────────────

                var usersTask =
                    _firebase.GetAllUsersAsync();

                var damageTask =
                    _firebase.GetAllDamageReportsRawAsync();

                var returnsTask =
                    _firebase.GetAllReturnRequestsRawAsync();

                var toolsTask =
                    _firebase.GetAllToolsAsync(
                        forceRefresh: true);

                var transactionsTask =
                    _firebase.GetAllTransactionsAsync(
                        forceRefresh: true);

                var projectsTask =
                    _firebase.GetAllProjectsAsync();

                await Task.WhenAll(
                    usersTask,
                    damageTask,
                    returnsTask,
                    toolsTask,
                    transactionsTask,
                    projectsTask);

                var users =
                    usersTask.Result;

                var damage =
                    damageTask.Result;

                var returns =
                    returnsTask.Result;

                var tools =
                    toolsTask.Result;

                var transactions =
                    transactionsTask.Result;

                var projects =
                    projectsTask.Result;

                // ─────────────────────────────────────────────
                // CURRENT PE PROJECTS
                // ─────────────────────────────────────────────

                var myProjectIds =
                    projects
                        .Where(p =>
                            !p.IsDeleted &&
                            p.CreatedBy ==
                                currentUser.UniqueKey)
                        .Select(p =>
                            p.ProjectId)
                        .ToHashSet();

                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationSync] " +
                    $"PE={currentUser.FullName} | " +
                    $"Projects={myProjectIds.Count}");

                // ─────────────────────────────────────────────
                // WORKER ACCOUNT APPROVALS
                // ─────────────────────────────────────────────
                //
                // Worker registration approval remains global.

                var pendingWorkers =
                    users.Count(u =>
                        u.Role == "Worker" &&
                        u.AccountStatus == "Pending");

                // ─────────────────────────────────────────────
                // DAMAGE REPORTS
                // ─────────────────────────────────────────────
                //
                // Only damage reports belonging to this
                // Project Engineer's projects.

                var pendingDamage =
                    damage.Count(r =>
                        r.Report.Status == "Pending" &&
                        myProjectIds.Contains(
                            r.Report.ProjectId));

                // ─────────────────────────────────────────────
                // RETURN REQUESTS
                // ─────────────────────────────────────────────

                var pendingReturns =
                    returns.Count(r =>
                        r.Request.Status == "Pending" &&
                        myProjectIds.Contains(
                            r.Request.ProjectId));

                // ─────────────────────────────────────────────
                // END-DAY CHECK-INS
                // ─────────────────────────────────────────────

                var pendingCheckIns =
                    tools.Count(t =>
                        t.Status == "Borrowed" &&
                        t.IsCheckInPending &&
                        myProjectIds.Contains(
                            t.BorrowedProjectId));

                var pendingReturnAndCheckIn =
                    pendingReturns +
                    pendingCheckIns;

                // ─────────────────────────────────────────────
                // TRANSACTIONS
                // ─────────────────────────────────────────────

                var pendingTransactions =
                    transactions.Count(t =>
                        t.Date.Date ==
                        DateTime.Today &&
                        (
                            string.IsNullOrWhiteSpace(
                                t.ProjectId) ||
                            myProjectIds.Contains(
                                t.ProjectId)
                        ));

                // ─────────────────────────────────────────────
                // DEBUG
                // ─────────────────────────────────────────────

                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationSync] " +
                    $"Pending Returns = {pendingReturns}");

                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationSync] " +
                    $"Pending Check-Ins = {pendingCheckIns}");

                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationSync] " +
                    $"Return/Check-In Total = " +
                    $"{pendingReturnAndCheckIn}");

                foreach (var item in returns.Where(r =>
                    r.Request.Status == "Pending" &&
                    myProjectIds.Contains(
                        r.Request.ProjectId)))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[RETURN] " +
                        $"{item.Request.ToolId} | " +
                        $"{item.Request.ToolName} | " +
                        $"{item.Request.ProjectName}");
                }

                foreach (var tool in tools.Where(t =>
                    t.Status == "Borrowed" &&
                    t.IsCheckInPending &&
                    myProjectIds.Contains(
                        t.BorrowedProjectId)))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[CHECK-IN] " +
                        $"{tool.ToolId} | " +
                        $"{tool.ToolName} | " +
                        $"{tool.BorrowedProjectName}");
                }

                // ─────────────────────────────────────────────
                // UPDATE NOTIFICATION STATE
                // ─────────────────────────────────────────────

                NotificationState.Instance.PendingWorkers =
                    pendingWorkers;

                NotificationState.Instance.PendingDamage =
                    pendingDamage;

                NotificationState.Instance.PendingReturnCheckIn =
                    pendingReturnAndCheckIn;

                NotificationState.Instance.PendingTransactions =
                    pendingTransactions;

                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationSync] " +
                    $"Workers={pendingWorkers} | " +
                    $"Damage={pendingDamage} | " +
                    $"Return/Check-In={pendingReturnAndCheckIn} | " +
                    $"Transactions={pendingTransactions}");

                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationSync] " +
                    $"TotalPending=" +
                    $"{NotificationState.Instance.TotalPending}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationSync] ERROR: " +
                    $"{ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────
        // LIVE SYNC
        // ─────────────────────────────────────────────────────────

        public void StartLiveSync()
        {
            _firebase.StartGlobalListenerDisposable(
                () =>
                {
                    _debounceCts?.Cancel();

                    _debounceCts =
                        new CancellationTokenSource();

                    var token =
                        _debounceCts.Token;

                    MainThread.BeginInvokeOnMainThread(
                        async () =>
                        {
                            try
                            {
                                // Force fresh information.
                                _firebase.InvalidateToolCache();
                                _firebase.InvalidateCatalogCache();
                                _firebase.InvalidateTransactionCache();

                                // Debounce rapid Firebase changes.
                                await Task.Delay(
                                    800,
                                    token);

                                await RefreshAsync();
                            }
                            catch (TaskCanceledException)
                            {
                                // Expected when another change
                                // arrives during debounce.
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"[NotificationSync] " +
                                    $"Live refresh error: " +
                                    $"{ex.Message}");
                            }
                        });
                });
        }

        // ─────────────────────────────────────────────────────────
        // CLEAR
        // ─────────────────────────────────────────────────────────

        private static void ClearNotifications()
        {
            NotificationState.Instance.PendingWorkers = 0;
            NotificationState.Instance.PendingDamage = 0;
            NotificationState.Instance.PendingReturnCheckIn = 0;
            NotificationState.Instance.PendingTransactions = 0;
        }
    }
}