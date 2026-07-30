using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.Views;

public partial class QrScannerView : ContentPage
{
    private readonly AuthService _auth;
    private bool _isProcessing = false;

    // ── Constructor — AuthService injected via DI ─────────────────────────────
    public QrScannerView(AuthService auth)
    {
        _auth = auth;
        InitializeComponent();

        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isProcessing = false;

        try
        {
            var status = await Permissions
                .RequestAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert(
                    "Camera Permission Required",
                    "StockGuard needs camera access to scan QR codes.",
                    "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            BarcodeReader.IsDetecting = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[QR] OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BarcodeReader.IsDetecting = false;
    }

    private void OnBarcodesDetected(
        object sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        BarcodeReader.IsDetecting = false;

        var result = e.Results?.FirstOrDefault();
        if (result is null)
        {
            _isProcessing = false;
            BarcodeReader.IsDetecting = true;
            return;
        }

        var scannedValue = result.Value;
        MainThread.BeginInvokeOnMainThread(async () =>
            await HandleScannedCode(scannedValue));
    }

    /// <summary>
    /// Routes after a successful scan based on the current user's role:
    ///
    ///   Worker          → WorkerToolDetailsView  (borrow / pause / transfer actions)
    ///   Project Engineer → AdminToolDetailsView   (read-only: info, borrower, history)
    ///
    /// Both are registered detail routes so GoToAsync("..") pops them correctly.
    /// </summary>
    private async Task HandleScannedCode(string toolId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(toolId))
            {
                await DisplayAlert(
                    "Invalid QR",
                    "Could not read a valid Tool ID.",
                    "OK");
                BarcodeReader.IsDetecting = true;
                _isProcessing = false;
                return;
            }

            var role = _auth.CurrentUser?.Role ?? "Worker";
            var encodedId = Uri.EscapeDataString(toolId);

            if (role == "Project Engineer")
            {
                // Admin — read-only tool detail with history
                await Shell.Current.GoToAsync(
                    $"{nameof(AdminToolDetailsView)}" +
                    $"?toolId={encodedId}");
            }
            else
            {
                // Worker — action page (borrow, pause, transfer, etc.)
                await Shell.Current.GoToAsync(
                    $"{nameof(WorkerToolDetailsView)}" +
                    $"?toolId={encodedId}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[QR] Navigation failed: {ex.Message}");
            await DisplayAlert(
                "Scan Error",
                $"Could not open tool details.\n{ex.Message}",
                "OK");
        }
        finally
        {
            _isProcessing = false;
            BarcodeReader.IsDetecting = true;
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}