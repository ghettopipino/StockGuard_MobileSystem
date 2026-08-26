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

    // Keeps track of tools already distributed
    // during this current QR bulk session.
    private readonly HashSet<string> _distributedToolIds =
        new(StringComparer.OrdinalIgnoreCase);

    // Keeps track of how many successful
    // distributions were made in this session.
    private int _distributedCount;

    // ─────────────────────────────────────────────────────────
    // QUERY PROPERTIES
    // ─────────────────────────────────────────────────────────

    // "" = normal scan
    // "AssignEquipment" = assign single equipment
    // "Distribute" = continuous bulk QR distribution
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
        _auth = auth;
        _firebase = firebase;

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

        _isProcessing = false;

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
    // MAIN SCAN HANDLER
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

        // ── ASSIGN EQUIPMENT ───────────────────────────────

        if (Mode ==
            "AssignEquipment")
        {
            await HandleAssignEquipmentScan(
                toolId);

            return;
        }

        // ── BULK DISTRIBUTE ────────────────────────────────

        if (Mode ==
            "Distribute")
        {
            await HandleDistributeScan(
                toolId);

            return;
        }

        // ── NORMAL SCAN ────────────────────────────────────

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
            System.Diagnostics.Debug.WriteLine(
                $"[QR] Navigation failed: " +
                $"{ex.Message}");

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
    // ASSIGN EQUIPMENT
    // ─────────────────────────────────────────────────────────

    private async Task HandleAssignEquipmentScan(
        string toolId)
    {
        try
        {
            var allTools =
                await _firebase
                    .GetAllToolsAsync(
                        forceRefresh: true);

            var tool =
                allTools.FirstOrDefault(t =>
                    t.ToolId ==
                    toolId);

            if (tool == null)
            {
                await DisplayAlert(
                    "Not Found",
                    $"No tool found with ID " +
                    $"{toolId}.",
                    "OK");

                return;
            }

            bool assigned =
                await WorkerAssignmentHelper
                    .AssignToolToWorkerViaPickerAsync(
                        _firebase,
                        _auth,
                        tool,
                        ProjectId);

            if (assigned)
            {
                await Shell.Current
                    .GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                $"Could not assign equipment.\n" +
                $"{ex.Message}",
                "OK");
        }
        finally
        {
            ResumeScanning();
        }
    }

    // ─────────────────────────────────────────────────────────
    // BULK QR DISTRIBUTION
    // ─────────────────────────────────────────────────────────

    private async Task HandleDistributeScan(
    string toolId)
    {
        try
        {
            // ─────────────────────────────────────────────
            // NORMALIZE SCANNED TOOL ID
            // ─────────────────────────────────────────────

            toolId = toolId?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(toolId))
            {
                await DisplayAlert(
                    "Invalid QR",
                    "Could not read a valid Tool ID.",
                    "OK");

                ResumeScanning();
                return;
            }

            // ─────────────────────────────────────────────
            // SESSION DUPLICATE CHECK
            // ─────────────────────────────────────────────

            bool alreadyScanned =
                _distributedToolIds.Any(id =>
                    string.Equals(
                        id?.Trim(),
                        toolId,
                        StringComparison.OrdinalIgnoreCase));

            if (alreadyScanned)
            {
                await DisplayAlert(
                    "Already Scanned",
                    $"Equipment {toolId} was already " +
                    "distributed during this session.",
                    "OK");

                ResumeScanning();
                return;
            }

            // ─────────────────────────────────────────────
            // FIND TOOL
            // ─────────────────────────────────────────────

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

            // Use the actual ToolId stored in Firebase
            // from this point onward.
            toolId = tool.ToolId;

            // ─────────────────────────────────────────────
            // CATALOG VALIDATION
            // ─────────────────────────────────────────────

            if (!string.IsNullOrWhiteSpace(CatalogId) &&
                !string.Equals(
                    tool.CatalogId?.Trim(),
                    CatalogId?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Wrong Equipment",
                    $"{tool.ToolName} ({tool.ToolId}) " +
                    "does not match the equipment category " +
                    "currently being distributed.",
                    "OK");

                ResumeScanning();
                return;
            }

            // ─────────────────────────────────────────────
            // TOOL STATUS
            // ─────────────────────────────────────────────

            if (!string.Equals(
                    tool.Status?.Trim(),
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                var assignedMessage =
                    !string.IsNullOrWhiteSpace(
                        tool.AssignedWorkerName)
                        ? $"\n\nCurrently assigned to: " +
                          $"{tool.AssignedWorkerName}"
                        : string.Empty;

                await DisplayAlert(
                    "Equipment Not Available",
                    $"{tool.ToolName} ({tool.ToolId}) " +
                    $"is currently {tool.Status}." +
                    assignedMessage,
                    "OK");

                ResumeScanning();
                return;
            }

            // ─────────────────────────────────────────────
            // PROJECT
            // ─────────────────────────────────────────────

            var projects =
                await _firebase
                    .GetAllProjectsAsync();

            var project =
                projects.FirstOrDefault(p =>
                    string.Equals(
                        p.ProjectId?.Trim(),
                        ProjectId?.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (project == null)
            {
                await DisplayAlert(
                    "Project Not Found",
                    "Could not find the project information.",
                    "OK");

                ResumeScanning();
                return;
            }

            // ─────────────────────────────────────────────
            // PROJECT WORKERS
            // ─────────────────────────────────────────────

            var workerKeys =
                await _firebase
                    .GetProjectWorkerKeysAsync(
                        ProjectId);

            var allUsers =
                await _firebase
                    .GetAllUsersAsync();

            var workers =
                allUsers
                    .Where(u =>
                        string.Equals(
                            u.Role,
                            "Worker",
                            StringComparison.OrdinalIgnoreCase) &&

                        string.Equals(
                            u.AccountStatus,
                            "Approved",
                            StringComparison.OrdinalIgnoreCase) &&

                        workerKeys.Any(key =>
                            string.Equals(
                                key?.Trim(),
                                u.UniqueKey?.Trim(),
                                StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(u =>
                        u.FullName)
                    .ToList();

            if (workers.Count == 0)
            {
                await DisplayAlert(
                    "No Workers",
                    "Assign workers to this project first.",
                    "OK");

                ResumeScanning();
                return;
            }

            // ─────────────────────────────────────────────
            // SELECT WORKER
            // ─────────────────────────────────────────────

            var workerNames =
                workers
                    .Select(w => w.FullName)
                    .ToArray();

            var selectedWorkerName =
                await DisplayActionSheet(
                    $"Assign {tool.ToolName} " +
                    $"({tool.ToolId})",
                    "Cancel",
                    null,
                    workerNames);

            if (string.IsNullOrWhiteSpace(
                    selectedWorkerName) ||
                selectedWorkerName == "Cancel")
            {
                ResumeScanning();
                return;
            }

            var worker =
                workers.FirstOrDefault(w =>
                    string.Equals(
                        w.FullName,
                        selectedWorkerName,
                        StringComparison.OrdinalIgnoreCase));

            if (worker == null)
            {
                ResumeScanning();
                return;
            }

            // ─────────────────────────────────────────────
            // CURRENT PROJECT ENGINEER
            // ─────────────────────────────────────────────

            var currentUser =
                _auth.CurrentUser;

            if (currentUser == null)
            {
                await DisplayAlert(
                    "Error",
                    "Current Project Engineer could not be identified.",
                    "OK");

                ResumeScanning();
                return;
            }

            // ─────────────────────────────────────────────
            // CREATE PRE-ASSIGNMENT
            // ─────────────────────────────────────────────
            //
            // IMPORTANT:
            // The tool remains Available until the
            // selected worker accepts the assignment.

            var assignment =
                new PreAssignment
                {
                    ToolId =
                        tool.ToolId,

                    ToolName =
                        tool.ToolName,

                    WorkerId =
                        worker.UniqueKey,

                    WorkerName =
                        worker.FullName,

                    ProjectId =
                        ProjectId,

                    ProjectName =
                        project.ProjectName,

                    AssignedById =
                        currentUser.UniqueKey,

                    AssignedByName =
                        currentUser.FullName,

                    Status =
                        "Pending",

                    DateCreated =
                        DateTime.Now
                };

            bool success =
                await _firebase
                    .CreatePreAssignmentAsync(
                        assignment);

            if (!success)
            {
                await DisplayAlert(
                    "Already Pending",
                    $"{tool.ToolName} ({tool.ToolId}) " +
                    "already has a pending assignment.\n\n" +
                    "The existing assignment must be " +
                    "accepted or declined before this " +
                    "equipment can be distributed again.",
                    "OK");

                ResumeScanning();
                return;
            }

            // ─────────────────────────────────────────────
            // MARK SUCCESSFUL SCAN
            // ─────────────────────────────────────────────

            if (!_distributedToolIds.Any(id =>
                    string.Equals(
                        id?.Trim(),
                        tool.ToolId?.Trim(),
                        StringComparison.OrdinalIgnoreCase)))
            {
                _distributedToolIds.Add(
                    tool.ToolId);
            }

            _distributedCount++;

            // ─────────────────────────────────────────────
            // CONTINUE OR FINISH
            // ─────────────────────────────────────────────

            bool scanAnother =
                await DisplayAlert(
                    "Distribution Sent",
                    $"{tool.ToolName} ({tool.ToolId}) → " +
                    $"{worker.FullName}\n\n" +
                    $"Distributed this session: " +
                    $"{_distributedCount}\n\n" +
                    "The equipment remains Available " +
                    "until the worker confirms receipt.",
                    "Scan Another",
                    "Finish");

            if (scanAnother)
            {
                ResumeScanning();
                return;
            }

            await FinishDistributionAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"HandleDistributeScan error: {ex.Message}");

            await DisplayAlert(
                "Error",
                $"Could not distribute equipment.\n" +
                $"{ex.Message}",
                "OK");

            ResumeScanning();
        }
    }

    // ─────────────────────────────────────────────────────────
    // FINISH BULK DISTRIBUTION
    // ─────────────────────────────────────────────────────────

    private async Task FinishDistributionAsync()
    {
        BarcodeReader.IsDetecting =
            false;

        _isProcessing =
            true;

        if (_distributedCount >
            0)
        {
            await DisplayAlert(
                "Distribution Complete",
                $"{_distributedCount} equipment " +
                $"item(s) were distributed.\n\n" +
                "Each worker must confirm receipt " +
                "before their equipment becomes Borrowed.",
                "OK");
        }

        await Shell.Current
            .GoToAsync("..");
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
                "Distribute" &&
            _distributedCount >
                0)
        {
            bool finish =
                await DisplayAlert(
                    "Finish Distribution",
                    $"You distributed " +
                    $"{_distributedCount} equipment " +
                    "item(s) in this session.\n\n" +
                    "Finish and return to the project?",
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