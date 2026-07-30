using System.Windows.Input;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly ThemeService _theme;

        // ── Theme icon ────────────────────────────────────────────
        public string ThemeIcon => _theme.IsDark ? "🌙" : "☀️";

        // ── Color properties ──────────────────────────────────────
        public Color PageBackground => _theme.IsDark
            ? Color.FromArgb("#080f1e")
            : Color.FromArgb("#f0f4f8");

        public Color TextPrimary => _theme.IsDark
            ? Colors.White
            : Color.FromArgb("#0f172a");

        public Color TextSecondary => _theme.IsDark
            ? Color.FromArgb("#94a3b8")
            : Color.FromArgb("#334155");

        public Color TextMuted => _theme.IsDark
            ? Color.FromArgb("#475569")
            : Color.FromArgb("#64748b");

        public Color DividerColor => _theme.IsDark
            ? Color.FromArgb("#1e293b")
            : Color.FromArgb("#e2e8f0");

        // ── Commands ──────────────────────────────────────────────
        public ICommand GetStartedCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public HomeViewModel(ThemeService theme)
        {
            _theme = theme;

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnPropertyChanged(nameof(ThemeIcon));
                    OnPropertyChanged(nameof(PageBackground));
                    OnPropertyChanged(nameof(TextPrimary));
                    OnPropertyChanged(nameof(TextSecondary));
                    OnPropertyChanged(nameof(TextMuted));
                    OnPropertyChanged(nameof(DividerColor));
                });

            ToggleThemeCommand = new Command(() => _theme.Toggle());
            GetStartedCommand = new Command(async () =>
                await Shell.Current.GoToAsync("//LoginView"));
        }
    }
}