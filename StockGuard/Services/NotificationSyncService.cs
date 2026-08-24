using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StockGuard.Services
{
    public class NotificationSyncService
    {
        private readonly FirebaseService _firebase;
        private CancellationTokenSource? _debounceCts;

        public NotificationSyncService(
            FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task RefreshAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "NotificationSync: Starting refresh...");

                // ── USERS ─────────────────────────────────────

                var users =
                    await _firebase.GetAllUsersAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"Users loaded: {users.Count}");

                // ── DAMAGE REPORTS ────────────────────────────

                var damage =
                    await _firebase
                        .GetAllDamageReportsRawAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"Damage loaded: {damage.Count}");

                // ── RETURN REQUESTS ───────────────────────────

                var returns =
                    await _firebase
                        .GetAllReturnRequestsRawAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"Returns loaded: {returns.Count}");

                // ── TOOLS / END-DAY CHECK-INS ────────────────

                var tools =
                    await _firebase
                        .GetAllToolsAsync(
                            forceRefresh: true);

                System.Diagnostics.Debug.WriteLine(
                    $"Tools loaded: {tools.Count}");

                // ── TRANSACTIONS ──────────────────────────────

                var transactions =
                    await _firebase
                        .GetAllTransactionsAsync(
                            forceRefresh: true);

                System.Diagnostics.Debug.WriteLine(
                    $"Transactions loaded: {transactions.Count}");

                // ─────────────────────────────────────────────
                // COUNTS
                // ─────────────────────────────────────────────

                var pendingWorkers =
                    users.Count(u =>
                        u.Role == "Worker" &&
                        u.AccountStatus == "Pending");

                var pendingDamage =
                    damage.Count(r =>
                        r.Report.Status == "Pending");

                var pendingReturns =
                    returns.Count(r =>
                        r.Request.Status == "Pending");

                var pendingCheckIns =
                    tools.Count(t =>
                        t.Status == "Borrowed" &&
                        t.IsCheckInPending);

                var pendingReturnAndCheckIn =
                    pendingReturns +
                    pendingCheckIns;

                var pendingTx =
                    transactions.Count(t =>
                        t.Date.Date ==
                        DateTime.Today);

                System.Diagnostics.Debug.WriteLine(
                    $"Counts — " +
                    $"Workers:{pendingWorkers} " +
                    $"Damage:{pendingDamage} " +
                    $"Return/Check-In:{pendingReturnAndCheckIn} " +
                    $"Transactions:{pendingTx}");

                // ─────────────────────────────────────────────
                // UPDATE NOTIFICATION STATE
                // ─────────────────────────────────────────────

                NotificationState.Instance.PendingWorkers =
                    pendingWorkers;

                NotificationState.Instance.PendingDamage =
                    pendingDamage;

                // We keep the existing property name for now
                // so we do not break other UI bindings.
                //
                // It now represents:
                // Pending Returns + Pending End-Day Check-Ins.
                NotificationState.Instance.PendingPause =
                    pendingReturnAndCheckIn;

                NotificationState.Instance.PendingTransactions =
                    pendingTx;

                System.Diagnostics.Debug.WriteLine(
                    $"HasAny: " +
                    $"{NotificationState.Instance.HasAny} " +
                    $"Total: " +
                    $"{NotificationState.Instance.TotalPending}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"NotificationSync ERROR: {ex.Message}");
            }
        }

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
                                // Force fresh data after Firebase changes.
                                _firebase.InvalidateToolCache();
                                _firebase.InvalidateCatalogCache();
                                _firebase.InvalidateTransactionCache();

                                await Task.Delay(
                                    800,
                                    token);

                                await RefreshAsync();
                            }
                            catch (TaskCanceledException)
                            {
                            }
                        });
                });
        }
    }
}