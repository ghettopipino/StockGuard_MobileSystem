using System.Linq;
using System.Threading.Tasks;
using StockGuard.Models;

namespace StockGuard.Services
{
    /// <summary>
    /// Single source of truth for "PE picks a worker on this project and
    /// borrows a tool for them." Used by QrScannerView (scan-to-assign)
    /// and AdminToolDetailsViewModel (assign from tool details).
    /// </summary>
    public static class WorkerAssignmentHelper
    {
        public static async Task<bool> AssignToolToWorkerViaPickerAsync(
            FirebaseService firebase,
            AuthService auth,
            Tool tool,
            string projectId)
        {
            if (tool.Status != "Available")
            {
                await Shell.Current.DisplayAlert(
                    "Not Available",
                    $"{tool.ToolName} ({tool.ToolId}) is currently " +
                    $"{tool.Status} and cannot be assigned right now.",
                    "OK");
                return false;
            }

            if (string.IsNullOrEmpty(projectId))
            {
                await Shell.Current.DisplayAlert(
                    "No Project",
                    "This action needs a project context to assign equipment to.",
                    "OK");
                return false;
            }

            var projects = await firebase.GetAllProjectsAsync();
            var project = projects.FirstOrDefault(p => p.ProjectId == projectId);

            if (project is null || project.Status == "Completed")
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Project",
                    "This project is no longer active.",
                    "OK");
                return false;
            }

            // ── Which worker on this project? ────────────────────────
            var workerKeys = await firebase.GetProjectWorkerKeysAsync(projectId);
            var allUsers = await firebase.GetAllUsersAsync();

            var workers = allUsers
                .Where(u =>
                    u.Role == "Worker" &&
                    u.AccountStatus == "Approved" &&
                    workerKeys.Contains(u.UniqueKey))
                .ToList();

            if (workers.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Workers on Project",
                    $"{project.ProjectName} has no workers assigned yet.\n\n" +
                    $"Assign workers to the project first.",
                    "OK");
                return false;
            }

            var workerNames = workers.Select(w => w.FullName).ToArray();
            var selectedWorkerName = await Shell.Current.DisplayActionSheet(
                $"Assign {tool.ToolName} ({tool.ToolId}) to:",
                "Cancel", null, workerNames);

            if (selectedWorkerName == null || selectedWorkerName == "Cancel")
                return false;

            var worker = workers.FirstOrDefault(w => w.FullName == selectedWorkerName);
            if (worker is null) return false;

            // ── CREATE PENDING ASSIGNMENT ─────────────────────────────

            var currentPE = auth.CurrentUser;

            if (currentPE == null)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    "Project Engineer could not be identified.",
                    "OK");

                return false;
            }

            var assignment = new PreAssignment
            {
                ToolId = tool.ToolId,
                ToolName = tool.ToolName,

                WorkerId = worker.UniqueKey,
                WorkerName = worker.FullName,

                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,

                AssignedById = currentPE.UniqueKey,
                AssignedByName = currentPE.FullName,

                Status = "Pending",
                DateCreated = DateTime.Now
            };

            bool success =
                await firebase.CreatePreAssignmentAsync(assignment);

            if (!success)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not assign {tool.ToolName} — it may have just " +
                    $"been borrowed by someone else.",
                    "OK");
                return false;
            }

            await Shell.Current.DisplayAlert(
                  "Assignment Sent",
                  $"{tool.ToolName} ({tool.ToolId}) was assigned to " +
                  $"{worker.FullName} for {project.ProjectName}.\n\n" +
                  $"The worker must confirm receipt before the equipment " +
                  $"becomes Borrowed.",
                  "OK");

            return true;
        }
    }
}