using Microsoft.Maui.Platform;
using Microsoft.Win32;
using StockGuard.Services;
using StockGuard.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StockGuard.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private bool _showPassword;
        public bool ShowPassword
        {
            get => _showPassword;
            set => SetProperty(ref _showPassword, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        // Bound directly to Button.Text — no x:Name needed
        public string ThemeIcon => _theme.IsDark ? "🌙" : "☀️";

        public ICommand LoginCommand { get; }
        public ICommand GoToRegisterCommand { get; }
        public ICommand TogglePasswordCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand GoBackCommand { get; }

        public LoginViewModel(AuthService auth, ThemeService theme)
        {
            Title = "Login";
            _auth = auth;
            _theme = theme;

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            LoginCommand = new Command(
                async () => await LoginAsync(), () => !IsBusy);

            GoToRegisterCommand = new Command(async () =>
                await Shell.Current.GoToAsync(nameof(RegisterView)));

            TogglePasswordCommand = new Command(() =>
                ShowPassword = !ShowPassword);

            ToggleThemeCommand = new Command(() => _theme.Toggle());
            GoBackCommand = new Command(async () =>
    await Shell.Current.GoToAsync($"//{nameof(HomeView)}"));
        }

        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var (success, message) =
                    await _auth.LoginAsync(Email, Password);

                if (!success) { ErrorMessage = message; return; }

                if (_auth.CurrentUser!.IsProjectEngineer)
                    await Shell.Current.GoToAsync(
                        $"//{nameof(EquipmentCatalogView)}");
                else
                    await Shell.Current.GoToAsync(
                        $"//{nameof(WorkerDashboardView)}");
            }
            finally { IsBusy = false; }
        }
    }
}

