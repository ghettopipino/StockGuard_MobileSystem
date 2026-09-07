using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    public class ResetPasswordViewModel : INotifyPropertyChanged
    {
        private readonly FirebaseService _firebase;

        private string _verificationCode = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;
        private bool _isBusy;


        // ── CONSTRUCTOR ──────────────────────────────────────────────────────

        public ResetPasswordViewModel(
            FirebaseService firebase)
        {
            _firebase = firebase;

            ResetPasswordCommand =
                new Command(
                    async () => await ResetPasswordAsync(),
                    () => !IsBusy);

            GoBackCommand =
                new Command(
                    async () =>
                        await Shell.Current.GoToAsync(".."));
        }


        // ── PROPERTIES ───────────────────────────────────────────────────────

        public string VerificationCode
        {
            get => _verificationCode;
            set
            {
                if (_verificationCode == value)
                    return;

                _verificationCode = value;
                OnPropertyChanged();
            }
        }


        public string NewPassword
        {
            get => _newPassword;
            set
            {
                if (_newPassword == value)
                    return;

                _newPassword = value;
                OnPropertyChanged();
            }
        }


        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (_confirmPassword == value)
                    return;

                _confirmPassword = value;
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


        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value)
                    return;

                _isBusy = value;
                OnPropertyChanged();

                ((Command)ResetPasswordCommand)
                    .ChangeCanExecute();
            }
        }


        // ── COMMANDS ─────────────────────────────────────────────────────────

        public ICommand ResetPasswordCommand { get; }

        public ICommand GoBackCommand { get; }


        // ── RESET PASSWORD ───────────────────────────────────────────────────

        private async Task ResetPasswordAsync()
        {
          

            if (IsBusy)
                return;

            ClearError();


            // ── VALIDATE CODE ────────────────────────────────────────────────

            if (string.IsNullOrWhiteSpace(
                    VerificationCode))
            {
                ShowError(
                    "Please enter the verification code.");

                return;
            }

            if (VerificationCode.Trim().Length != 6)
            {
                ShowError(
                    "Verification code must contain 6 digits.");

                return;
            }


            // ── VALIDATE PASSWORD ────────────────────────────────────────────

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ShowError(
                    "Please enter your new password.");

                return;
            }

            if (NewPassword.Length < 6)
            {
                ShowError(
                    "Password must contain at least 6 characters.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    ConfirmPassword))
            {
                ShowError(
                    "Please confirm your new password.");

                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ShowError(
                    "Passwords do not match.");

                return;
            }


            // ── GET RESET INFORMATION ────────────────────────────────────────

            var savedEmail =
                Preferences.Default.Get(
                    "PasswordResetEmail",
                    string.Empty);

            var savedCode =
                Preferences.Default.Get(
                    "PasswordResetCode",
                    string.Empty);

            var savedExpiry =
                Preferences.Default.Get(
                    "PasswordResetExpiry",
                    string.Empty);


            if (string.IsNullOrWhiteSpace(savedEmail) ||
                string.IsNullOrWhiteSpace(savedCode) ||
                string.IsNullOrWhiteSpace(savedExpiry))
            {
                ShowError(
                    "Password reset request is no longer valid. Please request a new code.");

                return;
            }


            // ── CHECK EXPIRATION ─────────────────────────────────────────────

            if (!DateTime.TryParse(
                    savedExpiry,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var expiry))
            {
                ClearResetData();

                ShowError(
                    "Password reset request is invalid. Please request a new code.");

                return;
            }

            if (DateTime.UtcNow > expiry.ToUniversalTime())
            {
                ClearResetData();

                ShowError(
                    "Verification code has expired. Please request a new code.");

                return;
            }



            // ── VERIFY CODE ──────────────────────────────────────────────────

            if (VerificationCode.Trim() != savedCode)
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Code",
                    "Incorrect verification code.",
                    "OK");

                ShowError(
                    "Incorrect verification code.");

                return;
            }


            IsBusy = true;

            try
            {
                // ── FIND USER ────────────────────────────────────────────────

                var user =
                    await _firebase.GetUserByEmailAsync(
                        savedEmail);

                if (user is null)
                {
                    ShowError(
                        "StockGuard account could not be found.");

                    return;
                }


                // ── UPDATE PASSWORD ──────────────────────────────────────────
                //
                // StockGuard currently uses its existing password storage
                // format. Password hashing can be migrated separately without
                // changing the reset workflow.

                user.Password = NewPassword;

                var updated =
                    await _firebase.UpdateUserAsync(
                        user);

                if (!updated)
                {
                    ShowError(
                        "Unable to reset your password. Please try again.");

                    return;
                }


                // ── REMOVE RESET DATA ────────────────────────────────────────

                ClearResetData();


                // ── SUCCESS ──────────────────────────────────────────────────

                await Shell.Current.DisplayAlert(
                    "Password Reset",
                    "Your password has been changed successfully. You can now sign in with your new password.",
                    "OK");


                // Return to Login
                await Shell.Current.GoToAsync(
                    "//LoginView");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Reset password error: {ex.Message}");

                ShowError(
                    "Something went wrong. Please try again.");
            }
            finally
            {
                IsBusy = false;
            }
        }


        // ── HELPERS ──────────────────────────────────────────────────────────

        private void ClearResetData()
        {
            Preferences.Default.Remove(
                "PasswordResetEmail");

            Preferences.Default.Remove(
                "PasswordResetCode");

            Preferences.Default.Remove(
                "PasswordResetExpiry");
        }


        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }


        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
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