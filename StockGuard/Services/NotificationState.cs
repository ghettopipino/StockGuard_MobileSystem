using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockGuard.Services
{
    public class NotificationState : INotifyPropertyChanged
    {
        public static readonly NotificationState Instance = new();

        private NotificationState()
        {
        }

        // ── DAMAGE ───────────────────────────────────────

        private int _pendingDamage;

        public int PendingDamage
        {
            get => _pendingDamage;
            set
            {
                if (_pendingDamage == value)
                    return;

                _pendingDamage = value;

                OnPropertyChanged();
                NotifyTotals();
            }
        }

        // ── WORKERS ──────────────────────────────────────

        private int _pendingWorkers;

        public int PendingWorkers
        {
            get => _pendingWorkers;
            set
            {
                if (_pendingWorkers == value)
                    return;

                _pendingWorkers = value;

                OnPropertyChanged();
                NotifyTotals();
            }
        }

        // ── RETURN + END-DAY CHECK-IN ────────────────────

        private int _pendingReturnCheckIn;

        public int PendingReturnCheckIn
        {
            get => _pendingReturnCheckIn;
            set
            {
                if (_pendingReturnCheckIn == value)
                    return;

                _pendingReturnCheckIn = value;

                OnPropertyChanged();
                NotifyTotals();
            }
        }

        // ── TRANSACTIONS ─────────────────────────────────

        private int _pendingTransactions;

        public int PendingTransactions
        {
            get => _pendingTransactions;
            set
            {
                if (_pendingTransactions == value)
                    return;

                _pendingTransactions = value;

                OnPropertyChanged();
                NotifyTotals();
            }
        }

        // ── TOTAL ────────────────────────────────────────

        public int TotalPending =>
            PendingDamage +
            PendingWorkers +
            PendingReturnCheckIn +
            PendingTransactions;

        public bool HasAny =>
            TotalPending > 0;

        private void NotifyTotals()
        {
            OnPropertyChanged(nameof(TotalPending));
            OnPropertyChanged(nameof(HasAny));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }
    }
}