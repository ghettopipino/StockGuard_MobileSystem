using System;
using System.Threading.Tasks;
using System.Windows.Input;
using StockGuard.Models;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    [QueryProperty(nameof(ToolId), "toolId")]
    [QueryProperty(nameof(ToolName), "toolName")]
    [QueryProperty(nameof(Status), "status")]
    [QueryProperty(nameof(CatalogName), "catalogName")]
    public class QrDisplayViewModel : BaseViewModel
    {
        private readonly ThemeService _theme;
        private readonly FirebaseService _firebase;

        // ── Query Properties ──────────────────────────────────────

        private string _toolId = string.Empty;
        public string ToolId
        {
            get => _toolId;
            set
            {
                SetProperty(ref _toolId, value);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    MainThread.BeginInvokeOnMainThread(
                        async () => await LoadToolAsync());
                }
            }
        }

        private string _toolName = string.Empty;
        public string ToolName
        {
            get => _toolName;
            set => SetProperty(ref _toolName, value);
        }

        private string _status = string.Empty;
        public string Status
        {
            get => Tool?.Status ?? _status;
            set
            {
                SetProperty(ref _status, value);
                OnPropertyChanged(nameof(Status));
            }
        }

        private string _catalogName = string.Empty;
        public string CatalogName
        {
            get => _catalogName;
            set => SetProperty(ref _catalogName, value);
        }

        // ── Actual Tool Data ──────────────────────────────────────

        private Tool? _tool;
        public Tool? Tool
        {
            get => _tool;
            set
            {
                SetProperty(ref _tool, value);

                OnPropertyChanged(nameof(Status));

                OnPropertyChanged(nameof(CurrentBorrowerName));
                OnPropertyChanged(nameof(CurrentProjectName));

                OnPropertyChanged(nameof(HoldProjectName));
                OnPropertyChanged(nameof(HoldLocation));
                OnPropertyChanged(nameof(LastBorrowerName));

                OnPropertyChanged(nameof(IsBorrowed));
                OnPropertyChanged(nameof(IsOnHold));
                OnPropertyChanged(nameof(IsAvailable));
            }
        }

        // ── Display Properties ────────────────────────────────────

        public string CurrentBorrowerName =>
            Tool?.AssignedWorkerName ?? string.Empty;

        public string CurrentProjectName =>
            Tool?.BorrowedProjectName ?? string.Empty;

        public string HoldProjectName =>
            Tool?.HoldProjectName ?? string.Empty;

        public string HoldLocation =>
            Tool?.HoldLocation ?? string.Empty;

        public string LastBorrowerName =>
            Tool?.LastBorrowerName ?? string.Empty;

        // ── Visibility Helpers ────────────────────────────────────

        public bool IsAvailable =>
            Tool?.IsAvailable == true;

        public bool IsBorrowed =>
            Tool?.IsBorrowed == true;

        public bool IsOnHold =>
            Tool?.IsOnHold == true;

        // ── Commands ──────────────────────────────────────────────

        public ICommand GoBackCommand { get; }
        public ICommand ShareCommand { get; }

        // ── Constructor ───────────────────────────────────────────

        public QrDisplayViewModel(
            FirebaseService firebase,
            ThemeService theme)
        {
            _firebase = firebase;
            _theme = theme;

            GoBackCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            ShareCommand = new Command(
                async () => await ShareQrAsync());
        }

        // ── Load Latest Tool Data ─────────────────────────────────

        private async Task LoadToolAsync()
        {
            if (string.IsNullOrWhiteSpace(ToolId))
                return;

            try
            {
                var tool = await _firebase
                    .GetToolByIdAsync(ToolId);

                if (tool == null)
                    return;

                Tool = tool;

                // Use latest Firebase values
                ToolName = tool.ToolName;

                // Status now comes from Tool,
                // but notify the UI after loading.
                OnPropertyChanged(nameof(Status));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Load QR Tool error: {ex.Message}");
            }
        }

        // ── Share QR ──────────────────────────────────────────────

        private async Task ShareQrAsync()
        {
            var details = string.Empty;

            // Borrowed
            if (IsBorrowed)
            {
                details =
                    $"\nStatus: Borrowed\n" +
                    $"Borrower: {CurrentBorrowerName}\n" +
                    $"Project: {CurrentProjectName}\n";
            }

            // On Hold
            else if (IsOnHold)
            {
                details =
                    $"\nStatus: On Hold\n" +
                    $"Project: {HoldProjectName}\n" +
                    $"Location: {HoldLocation}\n" +
                    $"Last Borrower: {LastBorrowerName}\n";
            }

            // Other status
            else
            {
                details =
                    $"\nStatus: {Status}\n";
            }

            await Share.Default.RequestAsync(
                new ShareTextRequest
                {
                    Title =
                        $"QR Code — {ToolName} ({ToolId})",

                    Text =
                        $"StockGuard Tool QR Code\n\n" +
                        $"Tool Name: {ToolName}\n" +
                        $"Tool ID: {ToolId}\n" +
                        $"Catalog: {CatalogName}\n" +
                        details +
                        $"\nScan this ID in the StockGuard app: {ToolId}"
                });
        }
    }
}