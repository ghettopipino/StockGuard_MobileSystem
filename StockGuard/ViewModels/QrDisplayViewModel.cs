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
                OnPropertyChanged(nameof(AssignedByName));

                OnPropertyChanged(nameof(CheckInLocation));
                OnPropertyChanged(nameof(CheckInDateDisplay));
                OnPropertyChanged(nameof(CheckInVerifiedBy));
                OnPropertyChanged(nameof(HasCheckInInfo));

                OnPropertyChanged(nameof(IsBorrowed));
                OnPropertyChanged(nameof(IsAvailable));
                OnPropertyChanged(nameof(IsPendingReturn));
                OnPropertyChanged(nameof(IsDamaged));
            }
        }

        // ── Current assignment ──────────────────────────────

        public string CurrentBorrowerName =>
            string.IsNullOrWhiteSpace(Tool?.AssignedWorkerName)
                ? "—"
                : Tool.AssignedWorkerName;

        public string CurrentProjectName =>
            string.IsNullOrWhiteSpace(Tool?.BorrowedProjectName)
                ? "—"
                : Tool.BorrowedProjectName;

        public string AssignedByName =>
            string.IsNullOrWhiteSpace(Tool?.AssignedByName)
                ? "—"
                : Tool.AssignedByName;

        // ── End Day Check-In ────────────────────────────────

        public string CheckInLocation =>
            string.IsNullOrWhiteSpace(Tool?.LastCheckInLocation)
                ? "—"
                : Tool.LastCheckInLocation;

        public string CheckInDateDisplay =>
            Tool?.LastCheckInDate.HasValue == true
                ? Tool.LastCheckInDate.Value
                    .ToString("MMM d, yyyy h:mm tt")
                : "—";

        public string CheckInVerifiedBy =>
            string.IsNullOrWhiteSpace(Tool?.LastCheckInVerifiedByName)
                ? Tool?.IsCheckInPending == true
                    ? "Pending verification"
                    : "—"
                : Tool.LastCheckInVerifiedByName;

        public bool HasCheckInInfo =>
            Tool?.LastCheckInDate.HasValue == true ||
            Tool?.IsCheckInPending == true;

        // ── Status helpers ─────────────────────────────────

        public bool IsAvailable =>
            Tool?.IsAvailable == true;

        public bool IsBorrowed =>
            Tool?.IsBorrowed == true;

        public bool IsPendingReturn =>
            Tool?.IsPendingReturn == true;

        public bool IsDamaged =>
            Tool?.IsDamaged == true;

        // ── Commands ───────────────────────────────────────

        public ICommand GoBackCommand { get; }
        public ICommand ShareCommand { get; }

        public QrDisplayViewModel(
            FirebaseService firebase,
            ThemeService theme)
        {
            _firebase = firebase;
            _theme = theme;

            GoBackCommand =
                new Command(
                    async () =>
                        await Shell.Current.GoToAsync(".."));

            ShareCommand =
                new Command(
                    async () =>
                        await ShareQrAsync());
        }

        private async Task LoadToolAsync()
        {
            if (string.IsNullOrWhiteSpace(ToolId))
                return;

            try
            {
                var tool =
                    await _firebase.GetToolByIdAsync(
                        ToolId);

                if (tool == null)
                    return;

                Tool = tool;
                ToolName = tool.ToolName;

                OnPropertyChanged(nameof(Status));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Load QR Tool error: {ex.Message}");
            }
        }

        private async Task ShareQrAsync()
        {
            var details =
                $"\nStatus: {Status}\n";

            if (Tool?.HasAssignmentInfo == true)
            {
                details +=
                    $"Worker: {CurrentBorrowerName}\n" +
                    $"Project: {CurrentProjectName}\n" +
                    $"Assigned By: {AssignedByName}\n";
            }

            if (HasCheckInInfo)
            {
                details +=
                    $"Last Check-In: {CheckInDateDisplay}\n" +
                    $"Stored At: {CheckInLocation}\n" +
                    $"Verified By: {CheckInVerifiedBy}\n";
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