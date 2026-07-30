using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly AuthService _auth;
        private readonly ThemeService _theme;

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phoneNumber = string.Empty;
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        private string _selectedRole = "Worker";
        public string SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        private bool _showPassword;
        public bool ShowPassword
        {
            get => _showPassword;
            set => SetProperty(ref _showPassword, value);
        }

        private bool _showConfirmPassword;
        public bool ShowConfirmPassword
        {
            get => _showConfirmPassword;
            set => SetProperty(ref _showConfirmPassword, value);
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

        public List<string> RoleOptions { get; } =
            new() { "Worker", "Project Engineer" };

        // Bound directly to Button.Text — no x:Name needed
        public string ThemeIcon => _theme.IsDark ? "🌙" : "☀️";

        public ICommand RegisterCommand { get; }
        public ICommand GoToLoginCommand { get; }
        public ICommand TogglePasswordCommand { get; }
        public ICommand ToggleConfirmCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public RegisterViewModel(AuthService auth, ThemeService theme)
        {
            Title = "Register";
            _auth = auth;
            _theme = theme;

            _theme.ThemeChanged += _ =>
                MainThread.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(ThemeIcon)));

            RegisterCommand = new Command(
                async () => await RegisterAsync(), () => !IsBusy);

            GoToLoginCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            TogglePasswordCommand = new Command(() =>
                ShowPassword = !ShowPassword);

            ToggleConfirmCommand = new Command(() =>
                ShowConfirmPassword = !ShowConfirmPassword);

            ToggleThemeCommand = new Command(() => _theme.Toggle());
        }

        private async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var (success, message) =
                    await _auth.RegisterAsync(
                        FullName, Email, PhoneNumber, Address, Password,
                        ConfirmPassword, SelectedRole);

                if (!success) { ErrorMessage = message; return; }

                await Shell.Current.DisplayAlert(
                    "Registration Successful", message, "OK");

                FullName = Email = Password =
                    ConfirmPassword =PhoneNumber = Address = string.Empty;
                SelectedRole = "Worker";

                await Shell.Current.GoToAsync("..");
            }
            finally { IsBusy = false; }
        }
    }
}


