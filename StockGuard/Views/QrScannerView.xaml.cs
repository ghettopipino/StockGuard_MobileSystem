using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using StockGuard.Services;
using StockGuard.Views;

namespace StockGuard.Views;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(ProjectId), "projectId")]
public partial class QrScannerView : ContentPage
{
    private readonly AuthService _auth;
    private readonly FirebaseService _firebase;
    private bool _isProcessing = false;

    // "" (default) = normal scan-to-view-details behavior, unchanged
    // "AssignEquipment" = scan identifies a tool to deploy + pre-assign
    public string Mode { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;

    public QrScannerView(AuthService auth, FirebaseService firebase)
    {
        _auth = auth;
        _firebase = firebase;
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

    private async Task HandleScannedCode(string toolId)
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

        // ── Branch: Assign Equipment mode ──────────────────────
        if (Mode == "AssignEquipment")
        {
            await HandleAssignEquipmentScan(toolId);
            return;
        }
        if (Mode == "Deploy")                    // ← NEW
        {
            await HandleDeployScan(toolId);
            return;
        }

        // ── Default: original scan-to-view-details behavior ────
        try
        {
            var role = _auth.CurrentUser?.Role ?? "Worker";
            var encodedId = Uri.EscapeDataString(toolId);

            if (role == "Project Engineer")
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(AdminToolDetailsView)}" +
                    $"?toolId={encodedId}");
            }
            else
            {
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
    private async Task HandleAssignEquipmentScan(string toolId)
    {
        try
        {
            var allTools = await _firebase.GetAllToolsAsync();
            var tool = allTools.FirstOrDefault(t => t.ToolId == toolId);

            if (tool is null)
            {
                await DisplayAlert("Not Found", $"No tool found with ID {toolId}.", "OK");
                return;
            }

            bool assigned = await WorkerAssignmentHelper.AssignToolToWorkerViaPickerAsync(
                _firebase, _auth, tool, ProjectId);

            if (assigned)
                await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not assign equipment.\n{ex.Message}", "OK");
        }
        finally
        {
            _isProcessing = false;
            BarcodeReader.IsDetecting = true;
        }
    }

    //// ── Assign Equipment scan handling ──────────────────────────
    //private async Task HandleAssignEquipmentScan(string toolId)
    //{
    //    try
    //    {
    //        var allTools = await _firebase.GetAllToolsAsync();
    //        var tool = allTools.FirstOrDefault(t => t.ToolId == toolId);

    //        if (tool is null)
    //        {
    //            await DisplayAlert(
    //                "Not Found",
    //                $"No tool found with ID {toolId}.",
    //                "OK");
    //            _isProcessing = false;
    //            BarcodeReader.IsDetecting = true;
    //            return;
    //        }

    //        if (tool.Status != "Available")
    //        {
    //            await DisplayAlert(
    //                "Not Available",
    //                $"{tool.ToolName} ({tool.ToolId}) is currently " +
    //                $"{tool.Status} and cannot be assigned right now.",
    //                "OK");
    //            _isProcessing = false;
    //            BarcodeReader.IsDetecting = true;
    //            return;
    //        }

    //        // Get workers on this project
    //        var workerKeys = await _firebase
    //            .GetProjectWorkerKeysAsync(ProjectId);

    //        var allUsers = await _firebase.GetAllUsersAsync();

    //        var workers = allUsers
    //            .Where(u =>
    //                u.Role == "Worker" &&
    //                u.AccountStatus == "Approved" &&
    //                workerKeys.Contains(u.UniqueKey))
    //            .ToList();

    //        if (workers.Count == 0)
    //        {
    //            await DisplayAlert(
    //                "No Workers",
    //                "Assign workers to this project first.",
    //                "OK");
    //            _isProcessing = false;
    //            BarcodeReader.IsDetecting = true;
    //            return;
    //        }

    //        var workerNames = workers.Select(w => w.FullName).ToArray();

    //        var selectedWorkerName = await DisplayActionSheet(
    //            $"Assign {tool.ToolName} ({tool.ToolId}) to:",
    //            "Cancel", null,
    //            workerNames);

    //        if (selectedWorkerName == null || selectedWorkerName == "Cancel")
    //        {
    //            _isProcessing = false;
    //            BarcodeReader.IsDetecting = true;
    //            return;
    //        }

    //        var worker = workers.FirstOrDefault(
    //            w => w.FullName == selectedWorkerName);

    //        if (worker is null)
    //        {
    //            _isProcessing = false;
    //            BarcodeReader.IsDetecting = true;
    //            return;
    //        }

    //        // Deploy + pre-assign in one shot
    //        await _firebase.DeployToolToProjectAsync(ProjectId, tool.ToolId);
    //        tool.ProjectId = ProjectId;
    //        await _firebase.UpdateToolAsync(tool);

    //        var projects = await _firebase.GetAllProjectsAsync();
    //        var project = projects.FirstOrDefault(p => p.ProjectId == ProjectId);

    //        await _firebase.PreAssignToolAsync(
    //            tool.ToolId,
    //            tool.ToolName,
    //            worker.UniqueKey,
    //            worker.FullName,
    //            ProjectId,
    //            project?.ProjectName ?? string.Empty,
    //            _auth.CurrentUser?.FullName ?? "Project Engineer");

    //        await DisplayAlert(
    //            "✅ Equipment Assigned",
    //            $"{tool.ToolName} ({tool.ToolId}) assigned to {worker.FullName}.\n\n" +
    //            $"They'll confirm receipt when they arrive at the site.",
    //            "OK");

    //        await Shell.Current.GoToAsync("..");
    //    }
    //    catch (Exception ex)
    //    {
    //        await DisplayAlert(
    //            "Error",
    //            $"Could not assign equipment.\n{ex.Message}",
    //            "OK");
    //        _isProcessing = false;
    //        BarcodeReader.IsDetecting = true;
    //    }
    

    private async void OnCloseClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");


    // ── Deploy scan handling (PE only, bulk) ─────────────────────
    private async Task HandleDeployScan(string toolId)
    {
        if (_auth.CurrentUser?.Role != "Project Engineer")
        {
            await DisplayAlert("Not Allowed",
                "Only Project Engineers can deploy equipment.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        try
        {
            var allTools = await _firebase.GetAllToolsAsync();
            var tool = allTools.FirstOrDefault(t => t.ToolId == toolId);

            if (tool is null)
            {
                await DisplayAlert("Not Found", $"No tool found with ID {toolId}.", "OK");
                return;
            }

            if (tool.ProjectId == ProjectId)
            {
                await DisplayAlert("Already Deployed",
                    $"{tool.ToolName} ({tool.ToolId}) is already on this project.", "OK");
                return;
            }

            await _firebase.DeployToolToProjectAsync(ProjectId, tool.ToolId);
            tool.ProjectId = ProjectId;
            await _firebase.UpdateToolAsync(tool);

            await DisplayAlert("✅ Deployed",
                $"{tool.ToolName} ({tool.ToolId}) added to the project.\n\nKeep scanning to add more.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not deploy tool.\n{ex.Message}", "OK");
        }
        finally
        {
            _isProcessing = false;
            BarcodeReader.IsDetecting = true;   // ← stays on the scanner, unlike Assign mode
     
        }

    }

}