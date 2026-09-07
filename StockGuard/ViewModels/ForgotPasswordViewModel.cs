using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.ViewModels
{
    public class ForgotPasswordViewModel : INotifyPropertyChanged
    {
        private readonly FirebaseService _firebase;
        private readonly PasswordResetService _passwordResetService;

        private string _email = string.Empty;
        private string _errorMessage = string.Empty;
        private string _successMessage = string.Empty;

        private bool _hasError;
        private bool _hasSuccess;
        private bool _isBusy;


        // ── CONSTRUCTOR ──────────────────────────────────────────────────────

        public ForgotPasswordViewModel(
            FirebaseService firebase,
            PasswordResetService passwordResetService)
        {
            _firebase = firebase;
            _passwordResetService = passwordResetService;

            SendCodeCommand =
                new Command(
                    async () => await SendCodeAsync(),
                    () => !IsBusy);

            GoBackCommand =
                new Command(
                    async () => await Shell.Current.GoToAsync(".."));
        }


        // ── PROPERTIES ───────────────────────────────────────────────────────

        public string Email
        {
            get => _email;
            set
            {
                if (_email == value)
                    return;

                _email = value;
                OnPropertyChanged();
            }
        }


        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage == value)
                    return;

                _errorMessage = value;
                OnPropertyChanged();
            }
        }


        public bool HasError
        {
            get => _hasError;
            set
            {
                if (_hasError == value)
                    return;

                _hasError = value;
                OnPropertyChanged();
            }
        }


        public string SuccessMessage
        {
            get => _successMessage;
            set
            {
                if (_successMessage == value)
                    return;

                _successMessage = value;
                OnPropertyChanged();
            }
        }


        public bool HasSuccess
        {
            get => _hasSuccess;
            set
            {
                if (_hasSuccess == value)
                    return;

                _hasSuccess = value;
                OnPropertyChanged();
            }
        }


        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value)
                    return;

                _isBusy = value;
                OnPropertyChanged();

                ((Command)SendCodeCommand)
                    .ChangeCanExecute();
            }
        }


        // ── COMMANDS ─────────────────────────────────────────────────────────

        public ICommand SendCodeCommand { get; }

        public ICommand GoBackCommand { get; }


        // ── SEND VERIFICATION CODE ──────────────────────────────────────────

        private async Task SendCodeAsync()
        {
            if (IsBusy)
                return;

            ClearMessages();

            var emailClean =
                Email?.Trim().ToLower() ?? string.Empty;


            // ── VALIDATE EMAIL ───────────────────────────────────────────────

            if (string.IsNullOrWhiteSpace(emailClean))
            {
                ShowError(
                    "Please enter your email address.");

                return;
            }


            if (!emailClean.Contains("@") ||
                !emailClean.Contains("."))
            {
                ShowError(
                    "Please enter a valid email address.");

                return;
            }


            IsBusy = true;

            try
            {
                // ── CHECK REGISTERED USER ───────────────────────────────────

                var user =
                    await _firebase.GetUserByEmailAsync(
                        emailClean);

                if (user is null)
                {
                    ShowError(
                        "No StockGuard account was found with this email.");

                    return;
                }


                // ── GENERATE OTP ─────────────────────────────────────────────

                var verificationCode =
                    Random.Shared.Next(
                        100000,
                        1000000)
                    .ToString();


                // ── SEND EMAIL ───────────────────────────────────────────────

                var sent =
                    await _passwordResetService
                        .SendVerificationCodeAsync(
                            emailClean,
                            verificationCode);

                if (!sent)
                {
                    ShowError(
                        "Unable to send the verification code. Please try again.");

                    return;
                }


                // TEMPORARY:
                // We will move this code into the verification/reset
                // workflow in the next step.
                Preferences.Default.Set(
                    "PasswordResetEmail",
                    emailClean);

                Preferences.Default.Set(
                    "PasswordResetCode",
                    verificationCode);

                Preferences.Default.Set(
                    "PasswordResetExpiry",
                    DateTime.UtcNow
                        .AddMinutes(10)
                        .ToString("O"));


                ShowSuccess(
                    "Verification code sent. Please check your email.");

                await Shell.Current.GoToAsync(
                 nameof(ResetPasswordView));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Forgot password error: {ex.Message}");

                ShowError(
                    "Something went wrong. Please try again.");
            }
            finally
            {
                IsBusy = false;
            }
        }


        // ── MESSAGE HELPERS ──────────────────────────────────────────────────

        private void ClearMessages()
        {
            HasError = false;
            ErrorMessage = string.Empty;

            HasSuccess = false;
            SuccessMessage = string.Empty;
        }


        private void ShowError(string message)
        {
            SuccessMessage = string.Empty;
            HasSuccess = false;

            ErrorMessage = message;
            HasError = true;
        }


        private void ShowSuccess(string message)
        {
            ErrorMessage = string.Empty;
            HasError = false;

            SuccessMessage = message;
            HasSuccess = true;
        }


        // ── PROPERTY CHANGED ─────────────────────────────────────────────────

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
        }
    }
}