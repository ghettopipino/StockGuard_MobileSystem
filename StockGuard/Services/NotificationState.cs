using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Services
{
    public class NotificationState : INotifyPropertyChanged
    {
        public static readonly NotificationState Instance = new();

        private NotificationState() { }

        private int _pendingDamage;
        public int PendingDamage
        {
            get => _pendingDamage;
            set
            {
                if (_pendingDamage == value) return;
                _pendingDamage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPending));
                OnPropertyChanged(nameof(HasAny));
            }
        }

        private int _pendingWorkers;
        public int PendingWorkers
        {
            get => _pendingWorkers;
            set
            {
                if (_pendingWorkers == value) return;
                _pendingWorkers = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPending));
                OnPropertyChanged(nameof(HasAny));
            }
        }

        private int _pendingPause;
        public int PendingPause
        {
            get => _pendingPause;
            set
            {
                if (_pendingPause == value) return;
                _pendingPause = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPending));
                OnPropertyChanged(nameof(HasAny));
            }
        }

        private int _pendingTransactions;
        public int PendingTransactions
        {
            get => _pendingTransactions;
            set
            {
                if (_pendingTransactions == value) return;
                _pendingTransactions = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPending));
                OnPropertyChanged(nameof(HasAny));
            }
        }

        public int TotalPending =>
            PendingDamage + PendingWorkers +
            PendingPause + PendingTransactions;

        public bool HasAny => TotalPending > 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(
            [CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(
                this, new PropertyChangedEventArgs(name));
    }
}
