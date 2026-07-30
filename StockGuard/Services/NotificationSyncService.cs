using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Services
{
    public class NotificationSyncService
    {
        private readonly FirebaseService _firebase;
        private CancellationTokenSource? _debounceCts;

        public NotificationSyncService(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task RefreshAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔔 NotificationSync: Starting refresh...");

                var users = await _firebase.GetAllUsersAsync();
                System.Diagnostics.Debug.WriteLine($"🔔 Users loaded: {users.Count}");

                var damage = await _firebase.GetAllDamageReportsRawAsync();
                System.Diagnostics.Debug.WriteLine($"🔔 Damage loaded: {damage.Count}");

                var pause = await _firebase.GetAllPauseRequestsRawAsync();
                System.Diagnostics.Debug.WriteLine($"🔔 Pause loaded: {pause.Count}");

                var transactions = await _firebase.GetAllTransactionsAsync();
                System.Diagnostics.Debug.WriteLine($"🔔 Transactions loaded: {transactions.Count}");

                var pendingWorkers = users
                    .Count(u => u.Role == "Worker" &&
                                u.AccountStatus == "Pending");

                var pendingDamage = damage
                    .Count(r => r.Report.Status == "Pending");

                var pendingPause = pause
                    .Count(r => r.Request.Status == "Pending");

                var pendingTx = transactions
                    .Count(t => t.Date.Date == DateTime.Today);

                System.Diagnostics.Debug.WriteLine(
                    $"🔔 Counts — Workers:{pendingWorkers} " +
                    $"Damage:{pendingDamage} " +
                    $"Pause:{pendingPause} " +
                    $"Transactions:{pendingTx}");

                NotificationState.Instance.PendingWorkers = pendingWorkers;
                NotificationState.Instance.PendingDamage = pendingDamage;
                NotificationState.Instance.PendingPause = pendingPause;
                NotificationState.Instance.PendingTransactions = pendingTx;

                System.Diagnostics.Debug.WriteLine(
                    $"🔔 HasAny: {NotificationState.Instance.HasAny} " +
                    $"Total: {NotificationState.Instance.TotalPending}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"🔔 NotificationSync ERROR: {ex.Message}");
            }
        }

        public void StartLiveSync()
        {
            _firebase.StartGlobalListenerDisposable(() =>
            {
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
                var token = _debounceCts.Token;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Invalidate cache so RefreshAsync gets fresh data
                        _firebase.InvalidateToolCache();
                        _firebase.InvalidateCatalogCache();

                        await Task.Delay(800, token);
                        await RefreshAsync();
                    }
                    catch (TaskCanceledException) { }
                });
            });
        }
    }
}
