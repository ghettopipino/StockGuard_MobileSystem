using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using StockGuard.Services;
using StockGuard.Models;

namespace StockGuard.Views;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(ProjectId), "projectId")]
[QueryProperty(nameof(CatalogId), "catalogId")]

public partial class QrScannerView : ContentPage
{
    private readonly AuthService _auth;
    private readonly FirebaseService _firebase;

    private bool _isProcessing;


    // ─────────────────────────────────────────────────────────
    // SESSION COUNTERS
    // ─────────────────────────────────────────────────────────

    private readonly HashSet<string>
        _borrowedToolIds =
            new(
                StringComparer.OrdinalIgnoreCase);

    private int _borrowedCount;


    private readonly HashSet<string>
        _distributedToolIds =
            new(
                StringComparer.OrdinalIgnoreCase);

    private int _distributedCount;


    // ─────────────────────────────────────────────────────────
    // QUERY PROPERTIES
    // ─────────────────────────────────────────────────────────

    // "" = normal scan
    // "AssignEquipment" = PE borrows physical equipment
    // "Distribute" = PE distributes already-borrowed equipment
    public string Mode { get; set; } =
        string.Empty;

    public string ProjectId { get; set; } =
        string.Empty;

    public string CatalogId { get; set; } =
        string.Empty;


    // ─────────────────────────────────────────────────────────
    // CONSTRUCTOR
    // ─────────────────────────────────────────────────────────

    public QrScannerView(
        AuthService auth,
        FirebaseService firebase)
    {
        _auth =
            auth;

        _firebase =
            firebase;

        InitializeComponent();

        BarcodeReader.Options =
            new BarcodeReaderOptions
            {
                Formats =
                    BarcodeFormats.All,

                AutoRotate =
                    true,

                Multiple =
                    false
            };
    }


    // ─────────────────────────────────────────────────────────
    // PAGE LIFECYCLE
    // ─────────────────────────────────────────────────────────

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _isProcessing =
            false;

        try
        {
            var status =
                await Permissions
                    .RequestAsync<
                        Permissions.Camera>();

            if (status !=
                PermissionStatus.Granted)
            {
                await DisplayAlert(
                    "Camera Permission Required",
                    "StockGuard needs camera access to scan QR codes.",
                    "OK");

                await Shell.Current
                    .GoToAsync("..");

                return;
            }

            BarcodeReader.IsDetecting =
                true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[QR] OnAppearing error: " +
                $"{ex.Message}");
        }
    }


    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        BarcodeReader.IsDetecting =
            false;
    }


    // ─────────────────────────────────────────────────────────
    // BARCODE DETECTION
    // ─────────────────────────────────────────────────────────

    private void OnBarcodesDetected(
        object sender,
        BarcodeDetectionEventArgs e)
    {
        if (_isProcessing)
            return;

        _isProcessing =
            true;

        BarcodeReader.IsDetecting =
            false;

        var result =
            e.Results?.FirstOrDefault();

        if (result == null)
        {
            ResumeScanning();
            return;
        }

        var scannedValue =
            result.Value?.Trim();

        MainThread.BeginInvokeOnMainThread(
            async () =>
                await HandleScannedCode(
                    scannedValue ??
                    string.Empty));
    }


    // ─────────────────────────────────────────────────────────
    // MAIN HANDLER
    // ─────────────────────────────────────────────────────────

    private async Task HandleScannedCode(
        string toolId)
    {
        if (string.IsNullOrWhiteSpace(
                toolId))
        {
            await DisplayAlert(
                "Invalid QR",
                "Could not read a valid Tool ID.",
                "OK");

            ResumeScanning();

            return;
        }


        if (Mode ==
            "AssignEquipment")
        {
            await HandleBorrowEquipmentScan(
                toolId);

            return;
        }


        if (Mode ==
            "Distribute")
        {
            await HandleDistributeScan(
                toolId);

            return;
        }


        await HandleNormalScan(
            toolId);
    }


    // ─────────────────────────────────────────────────────────
    // NORMAL SCAN
    // ─────────────────────────────────────────────────────────

    private async Task HandleNormalScan(
        string toolId)
    {
        try
        {
            var role =
                _auth.CurrentUser?.Role ??
                "Worker";

            var encodedId =
                Uri.EscapeDataString(
                    toolId);

            if (role ==
                "Project Engineer")
            {
                await Shell.Current
                    .GoToAsync(
                        $"{nameof(AdminToolDetailsView)}" +
                        $"?toolId={encodedId}");
            }
            else
            {
                await Shell.Current
                    .GoToAsync(
                        $"{nameof(WorkerToolDetailsView)}" +
                        $"?toolId={encodedId}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Scan Error",
                $"Could not open tool details.\n" +
                $"{ex.Message}",
                "OK");
        }
        finally
        {
            ResumeScanning();
        }
    }


    // ─────────────────────────────────────────────────────────
    // PE BORROWS EQUIPMENT FROM OFFICE
    // ─────────────────────────────────────────────────────────

    private async Task HandleBorrowEquipmentScan(
        string toolId)
    {
        try
        {
            toolId =
                toolId.Trim();


            if (_borrowedToolIds.Contains(
                    toolId))
            {
                await DisplayAlert(
                    "Already Scanned",
                    $"{toolId} was already borrowed during " +
                    "this scanning session.",
                    "OK");

                ResumeScanning();

                return;
            }


            var allTools =
                await _firebase
                    .GetAllToolsAsync(
                        forceRefresh: true);

            var tool =
                allTools.FirstOrDefault(t =>
                    string.Equals(
                        t.ToolId?.Trim(),
                        toolId,
                        StringComparison.OrdinalIgnoreCase));

            if (tool == null)
            {
                await DisplayAlert(
                    "Not Found",
                    $"No tool found with ID {toolId}.",
                    "OK");

                ResumeScanning();

                return;
            }


            // Optional catalog passed from Project Details.
            if (!string.IsNullOrWhiteSpace(
                    CatalogId) &&
                !string.Equals(
                    tool.CatalogId,
                    CatalogId,
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Wrong Equipment",
                    $"{tool.ToolName} ({tool.ToolId}) does not " +
                    "match the equipment requirement you selected.",
                    "OK");

                ResumeScanning();

                return;
            }


            if (!string.Equals(
                    tool.Status,
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Equipment Not Available",
                    $"{tool.ToolName} ({tool.ToolId}) is currently " +
                    $"{tool.Status}.\n\n" +
                    "Only Available equipment can be borrowed " +
                    "from the office.",
                    "OK");

                ResumeScanning();

                return;
            }


            var currentPE =
                _auth.CurrentUser;

            if (currentPE == null)
            {
                await DisplayAlert(
                    "Error",
                    "Current Project Engineer could not be identified.",
                    "OK");

                ResumeScanning();

                return;
            }


            string result =
                await _firebase
                    .BorrowToolIntoProjectAsync(
                        tool.ToolId,
                        ProjectId,
                        currentPE.UniqueKey,
                        currentPE.FullName);


            if (result ==
                "NOT_REQUIRED")
            {
                await DisplayAlert(
                    "Equipment Not Required",
                    $"{tool.ToolName} has not been added to " +
                    "this project's equipment requirements.",
                    "OK");

                ResumeScanning();

                return;
            }


            if (result ==
                "REQUIREMENT_FULFILLED")
            {
                await DisplayAlert(
                    "Requirement Fulfilled",
                    $"This project already has the required " +
                    $"number of {tool.ToolName} units.",
                    "OK");

                ResumeScanning();

                return;
            }


            if (result ==
                "NOT_AVAILABLE")
            {
                await DisplayAlert(
                    "Equipment Not Available",
                    $"{tool.ToolName} ({tool.ToolId}) is no longer available.",
                    "OK");

                ResumeScanning();

                return;
            }


            if (result ==
                "INVALID_PROJECT")
            {
                await DisplayAlert(
                    "Invalid Project",
                    "The project could not be found or is already completed.",
                    "OK");

                await Shell.Current
                    .GoToAsync("..");

                return;
            }


            if (result !=
                "SUCCESS")
            {
                await DisplayAlert(
                    "Error",
                    "Could not borrow the equipment.",
                    "OK");

                ResumeScanning();

                return;
            }


            _borrowedToolIds.Add(
                tool.ToolId);

            _borrowedCount++;


            bool scanAnother =
                await DisplayAlert(
                    "Equipment Borrowed",
                    $"{tool.ToolName} ({tool.ToolId}) is now Borrowed.\n\n" +
                    $"Project Engineer accountability is active.\n" +
                    $"Borrowed this session: {_borrowedCount}",
                    "Scan Another",
                    "Finish");


            if (scanAnother)
            {
                ResumeScanning();

                return;
            }


            await Shell.Current
                .GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                $"Could not borrow equipment.\n" +
                $"{ex.Message}",
                "OK");

            ResumeScanning();
        }
    }


    // ─────────────────────────────────────────────────────────
    // DISTRIBUTE ALREADY-BORROWED EQUIPMENT
    // ─────────────────────────────────────────────────────────

    private async Task HandleDistributeScan(
        string toolId)
    {
        try
        {
            toolId =
                toolId?.Trim() ??
                string.Empty;


            if (string.IsNullOrWhiteSpace(
                    toolId))
            {
                await DisplayAlert(
                    "Invalid QR",
                    "Could not read a valid Tool ID.",
                    "OK");

                ResumeScanning();

                return;
            }


            if (_distributedToolIds.Contains(
                    toolId))
            {
                await DisplayAlert(
                    "Already Scanned",
                    $"{toolId} was already distributed " +
                    "during this session.",
                    "OK");

                ResumeScanning();

                return;
            }


            var allTools =
                await _firebase
                    .GetAllToolsAsync(
                        forceRefresh: true);

            var tool =
                allTools.FirstOrDefault(t =>
                    string.Equals(
                        t.ToolId?.Trim(),
                        toolId,
                        StringComparison.OrdinalIgnoreCase));

            if (tool == null)
            {
                await DisplayAlert(
                    "Not Found",
                    $"No tool found with ID {toolId}.",
                    "OK");

                ResumeScanning();

                return;
            }


            toolId =
                tool.ToolId;


            // Must match selected catalog.
            if (!string.IsNullOrWhiteSpace(
                    CatalogId) &&
                !string.Equals(
                    tool.CatalogId,
                    CatalogId,
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Wrong Equipment",
                    $"{tool.ToolName} ({tool.ToolId}) does not " +
                    "match the selected equipment category.",
                    "OK");

                ResumeScanning();

                return;
            }


            // IMPORTANT:
            // Distribution now requires Borrowed, not Available.
            if (!string.Equals(
                    tool.Status,
                    "Borrowed",
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Borrow Equipment First",
                    $"{tool.ToolName} ({tool.ToolId}) is currently " +
                    $"{tool.Status}.\n\n" +
                    "The Project Engineer must borrow the equipment " +
                    "from the office before distributing it.",
                    "OK");

                ResumeScanning();

                return;
            }


            if (!string.Equals(
                    tool.BorrowedProjectId,
                    ProjectId,
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Wrong Project",
                    $"{tool.ToolName} ({tool.ToolId}) was not borrowed " +
                    "for this project.",
                    "OK");

                ResumeScanning();

                return;
            }


            if (!string.IsNullOrWhiteSpace(
                    tool.AssignedWorkerId))
            {
                await DisplayAlert(
                    "Already Distributed",
                    $"{tool.ToolName} ({tool.ToolId}) is already " +
                    $"assigned to {tool.AssignedWorkerName}.",
                    "OK");

                ResumeScanning();

                return;
            }


            bool assigned =
                await WorkerAssignmentHelper
                    .AssignToolToWorkerViaPickerAsync(
                        _firebase,
                        _auth,
                        tool,
                        ProjectId);


            if (!assigned)
            {
                ResumeScanning();

                return;
            }


            _distributedToolIds.Add(
                tool.ToolId);

            _distributedCount++;


            bool scanAnother =
                await DisplayAlert(
                    "Distribution Pending",
                    $"{tool.ToolName} ({tool.ToolId}) was sent " +
                    "for worker confirmation.\n\n" +
                    $"Distributed this session: {_distributedCount}\n\n" +
                    "The Project Engineer remains accountable " +
                    "until the worker accepts.",
                    "Scan Another",
                    "Finish");


            if (scanAnother)
            {
                ResumeScanning();

                return;
            }


            await Shell.Current
                .GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                $"Could not distribute equipment.\n" +
                $"{ex.Message}",
                "OK");

            ResumeScanning();
        }
    }


    // ─────────────────────────────────────────────────────────
    // RESUME CAMERA
    // ─────────────────────────────────────────────────────────

    private void ResumeScanning()
    {
        _isProcessing =
            false;

        if (BarcodeReader !=
            null)
        {
            BarcodeReader.IsDetecting =
                true;
        }
    }


    // ─────────────────────────────────────────────────────────
    // CLOSE
    // ─────────────────────────────────────────────────────────

    private async void OnCloseClicked(
        object sender,
        EventArgs e)
    {
        BarcodeReader.IsDetecting =
            false;


        if (Mode ==
                "AssignEquipment" &&
            _borrowedCount >
                0)
        {
            bool finish =
                await DisplayAlert(
                    "Finish Borrowing",
                    $"You borrowed {_borrowedCount} equipment " +
                    "item(s) during this session.",
                    "Finish",
                    "Continue Scanning");

            if (!finish)
            {
                ResumeScanning();

                return;
            }
        }


        if (Mode ==
                "Distribute" &&
            _distributedCount >
                0)
        {
            bool finish =
                await DisplayAlert(
                    "Finish Distribution",
                    $"You distributed {_distributedCount} equipment " +
                    "item(s) during this session.",
                    "Finish",
                    "Continue Scanning");

            if (!finish)
            {
                ResumeScanning();

                return;
            }
        }


        await Shell.Current
            .GoToAsync("..");
    }
}