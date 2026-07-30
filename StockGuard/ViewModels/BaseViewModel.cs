using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace StockGuard.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        // ── Busy / Title ──────────────────────────────────────────────────────
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        // ── Global Shell Commands (inherited by every ViewModel) ──────────────

        /// <summary>
        /// Opens the flyout sidebar from any page.
        /// Bind to a ☰ hamburger button in your custom header.
        /// This is necessary because Shell.NavBarIsVisible="False" hides
        /// the Shell NavBar — and with it, the only built-in hamburger button.
        /// Without this command, the user has no way to reopen the sidebar.
        /// </summary>
        public ICommand OpenFlyoutCommand { get; } =
            new Command(() => Shell.Current.FlyoutIsPresented = true);

        /// <summary>
        /// Pops the current page off the Shell navigation stack.
        /// Bind to a ← back button in custom headers on detail pages.
        ///
        /// Why this works and a plain async Command sometimes doesn't:
        /// - new Command(async () => ...) swallows async exceptions silently.
        /// - Using Shell.Current.GoToAsync("..") here plus a Clicked fallback
        ///   in the code-behind guarantees the navigation always fires.
        ///
        /// Only valid on detail pages (registered routes).
        /// Do NOT call this on FlyoutItem root pages — there is nothing to pop.
        /// </summary>
        public ICommand GoBackCommand { get; } =
            new Command(async () =>
            {
                try
                {
                    await Shell.Current.GoToAsync("..");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[GoBack] Navigation error: {ex.Message}");
                }
            });

        // ── INotifyPropertyChanged ────────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(
                this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<T>(
            ref T field, T value,
            [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}