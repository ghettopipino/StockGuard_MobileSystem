using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockGuard.Models
{
    public class SelectableItem : INotifyPropertyChanged
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CheckIcon));
                OnPropertyChanged(nameof(CardBorderColor));
                OnPropertyChanged(nameof(CardBackground));
            }
        }

        public string CheckIcon =>
            IsSelected ? "✅" : "⬜";

        public string CardBorderColor =>
            IsSelected ? "#3b82f6" : "#1e3a5f";

        public string CardBackground =>
            IsSelected ? "#0f1c35" : "#0d1526";

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(name));
    }
}
