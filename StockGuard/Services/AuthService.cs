using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using StockGuard.Models;

namespace StockGuard.Services
{
    public class AuthService
    {
        private readonly FirebaseService _firebase;

        public User? CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;

        public AuthService(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        // ── LOGIN ─────────────────────────────────────────────────

        public async Task<(bool Success, string Message)>
    LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
                return (false,
                    "Please enter your email and password.");

            try
            {
                var emailClean = email.Trim().ToLower();
                var user = await _firebase
                    .GetUserByEmailAsync(emailClean);

                if (user is null)
                    return (false, "Incorrect email or password.");

                if (user.Password != password)
                    return (false, "Incorrect email or password.");

                if (user.AccountStatus == "Pending")
                    return (false,
                        "Your account is pending approval " +
                        "by the Project Engineer.");

                if (user.AccountStatus == "Rejected")
                    return (false,
                        "Your account has been rejected. " +
                        "Please contact the Project Engineer.");

                // ✅ Debug log to verify UniqueKey
                System.Diagnostics.Debug.WriteLine(
                    $"Login success: {user.Email} | " +
                    $"UniqueKey: {user.UniqueKey}");

                CurrentUser = user;
                return (true, "Login successful.");
            }
            catch (Exception ex)
            {
                return (false,
                    $"Connection error. Please check your " +
                    $"internet connection.\n\n{ex.Message}");
            }
        }

        // ── REGISTER ──────────────────────────────────────────────

        public async Task<(bool Success, string Message)>
            RegisterAsync(string fullName, string email, string phoneNumber, string address,
                string password, string confirmPassword,
                string role)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Full name is required.");

            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email is required.");

            if (!email.Contains("@") || !email.Contains("."))
                return (false,
                    "Please enter a valid email address.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return (false, "Phone number is required.");

            if (string.IsNullOrWhiteSpace(address))
                return (false, "Address is required.");

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required.");

            if (password.Length < 6)
                return (false,
                    "Password must be at least 6 characters.");

            if (password != confirmPassword)
                return (false, "Passwords do not match.");


            try
            {
                var emailClean = email.Trim().ToLower();

                // Check if email already exists
                var existing = await _firebase
                    .GetUserByEmailAsync(emailClean);

                if (existing is not null)
                    return (false,
                        "An account with this email already exists.");

                // ✅ Use timestamp as unique ID
                // Guarantees no collision ever
                var uniqueId = DateTimeOffset.UtcNow
                    .ToUnixTimeMilliseconds();

                var newUser = new User
                {
                    Id = (int)(uniqueId % int.MaxValue),
                    UniqueKey = uniqueId.ToString(),
                    FullName = fullName.Trim(),
                    Email = emailClean,
                    PhoneNumber = phoneNumber.Trim(),
                    Address = address.Trim(),
                    Password = password,
                    Role = role,
                    AccountStatus = "Pending",
                    DateCreated = DateTime.Now,
                    IsDeleted = false
                };

                // ✅ Use UniqueKey as Firebase node key
                // so accounts never overwrite each other
                await _firebase.CreateUserWithKeyAsync(newUser);

                return (true,
                    role == "Project Engineer"
                        ? "Registration successful. Your Project " +
                          "Engineer account is pending approval."
                        : "Registration successful. Your account " +
                          "is pending approval by the Project Engineer.");
            }
            catch (Exception ex)
            {
                return (false,
                    $"Registration failed. Please check your " +
                    $"internet connection.\n\n{ex.Message}");
            }
        }

        // ── LOGOUT ────────────────────────────────────────────────

        public void Logout()
        {
            CurrentUser = null;
        }

        // ── SEED DEFAULT ACCOUNTS ─────────────────────────────────

        public async Task SeedDefaultAccountsAsync()
        {
            try
            {
                // ✅ Check by email — not by count
                var pe = await _firebase
                    .GetUserByEmailAsync("pe@stockguard.com");

                if (pe is null)
                {
                    await _firebase.CreateUserWithKeyAsync(
                        new User
                        {
                            Id = 1,
                            UniqueKey = "pe-default",
                            FullName = "Project Engineer",
                            Email = "pe@stockguard.com",
                            Password = "admin123",
                            Role = "Project Engineer",
                            AccountStatus = "Approved",
                            DateCreated = DateTime.Now,
                            IsDeleted = false
                        });
                }

                var worker = await _firebase
                    .GetUserByEmailAsync("worker@stockguard.com");

                if (worker is null)
                {
                    await _firebase.CreateUserWithKeyAsync(
                        new User
                        {
                            Id = 2,
                            UniqueKey = "worker-default",
                            FullName = "Juan Dela Cruz",
                            Email = "worker@stockguard.com",
                            Password = "worker123",
                            Role = "Worker",
                            AccountStatus = "Approved",
                            DateCreated = DateTime.Now,
                            IsDeleted = false
                        });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Seed error: {ex.Message}");
            }
        }

        // ── GET ALL USERS ─────────────────────────────────────────

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _firebase.GetAllUsersAsync();
        }
    }
}


