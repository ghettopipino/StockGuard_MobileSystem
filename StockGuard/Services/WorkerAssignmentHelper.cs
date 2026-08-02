using System.Linq;
using System.Threading.Tasks;
using StockGuard.Models;

namespace StockGuard.Services
{
    /// <summary>
    /// Single source of truth for "PE picks a worker on this project and
    /// pre-assigns a tool to them." Used by QrScannerView (scan-to-assign),
    /// AdminToolDetailsViewModel (assign from tool details), and can replace
    /// the old per-tool picker in ProjectDetailsViewModel.
    /// </summary>
    public static class WorkerAssignmentHelper
    {
        public static async Task<bool> AssignToolToWorkerViaPickerAsync(
            FirebaseService firebase,
            AuthService auth,
            Tool tool,
            string projectId)   // kept for signature compatibility; no longer required
        {
            System.Diagnostics.Debug.WriteLine(
       $"[AssignHelper] ToolId={tool.ToolId} ProjectId='{tool.ProjectId}' Status={tool.Status}");

            if (tool.Status != "Available")
            {
                await Shell.Current.DisplayAlert(
                    "Not Available",
                    $"{tool.ToolName} ({tool.ToolId}) is currently " +
                    $"{tool.Status} and cannot be assigned right now.",
                    "OK");
                return false;
            }

            // ── Step 1: which project? ──────────────────────────────
            var projects = await firebase.GetAllProjectsAsync();
            Project? project;

            if (string.IsNullOrEmpty(tool.ProjectId))
            {
                var eligible = projects
                    .Where(p => p.Status != "Completed")
                    .ToList();

                if (eligible.Count == 0)
                {
                    await Shell.Current.DisplayAlert(
                        "No Projects",
                        "Create a project first before deploying equipment.",
                        "OK");
                    return false;
                }

                var projectNames = eligible.Select(p => p.ProjectName).ToArray();
                var selectedProjectName = await Shell.Current.DisplayActionSheet(
                    $"Deploy {tool.ToolName} ({tool.ToolId}) to which project?",
                    "Cancel", null, projectNames);

                if (selectedProjectName == null || selectedProjectName == "Cancel")
                    return false;

                project = eligible.FirstOrDefault(p => p.ProjectName == selectedProjectName);
                if (project is null) return false;

                await firebase.DeployToolToProjectAsync(project.ProjectId, tool.ToolId);
                tool.ProjectId = project.ProjectId;
                await firebase.UpdateToolAsync(tool);
            }
            else
            {
                project = projects.FirstOrDefault(p => p.ProjectId == tool.ProjectId);

                // Stale/dead reference — project was completed or deleted
                // after this tool was deployed to it. Treat as undeployed.
                if (project is null || project.Status == "Completed")
                {
                    var eligible = projects
                        .Where(p => p.Status != "Completed")
                        .ToList();

                    if (eligible.Count == 0)
                    {
                        await Shell.Current.DisplayAlert(
                            "No Projects",
                            "Create a project first before deploying equipment.",
                            "OK");
                        return false;
                    }

                    var projectNames = eligible.Select(p => p.ProjectName).ToArray();
                    var selectedProjectName = await Shell.Current.DisplayActionSheet(
                        $"{tool.ToolName} ({tool.ToolId}) is not on an active project. Deploy to which project?",
                        "Cancel", null, projectNames);

                    if (selectedProjectName == null || selectedProjectName == "Cancel")
                        return false;

                    project = eligible.FirstOrDefault(p => p.ProjectName == selectedProjectName);
                    if (project is null) return false;

                    await firebase.DeployToolToProjectAsync(project.ProjectId, tool.ToolId);
                    tool.ProjectId = project.ProjectId;
                    await firebase.UpdateToolAsync(tool);
                }
            }

            var targetProjectId = tool.ProjectId;

            // ── Step 2: does that project have workers? ─────────────
            var workerKeys = await firebase.GetProjectWorkerKeysAsync(targetProjectId);
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
                    $"{project?.ProjectName ?? "This project"} has no workers assigned yet.\n\n" +
                    $"Assign workers to the project before deploying equipment.",
                    "OK");
                return false;
            }

            // ── Step 3: which worker? ────────────────────────────────
            var workerNames = workers.Select(w => w.FullName).ToArray();
            var selectedWorkerName = await Shell.Current.DisplayActionSheet(
                $"Assign {tool.ToolName} ({tool.ToolId}) to:",
                "Cancel", null, workerNames);

            if (selectedWorkerName == null || selectedWorkerName == "Cancel")
                return false;

            var worker = workers.FirstOrDefault(w => w.FullName == selectedWorkerName);
            if (worker is null) return false;

            await firebase.PreAssignToolAsync(
                tool.ToolId,
                tool.ToolName,
                worker.UniqueKey,
                worker.FullName,
                targetProjectId,
                project?.ProjectName ?? string.Empty,
                auth.CurrentUser?.FullName ?? "Project Engineer");

            await Shell.Current.DisplayAlert(
                "✅ Equipment Assigned",
                $"{tool.ToolName} ({tool.ToolId}) assigned to {worker.FullName} " +
                $"on {project?.ProjectName}.\n\nThey'll see it in Pending Assignments to accept or decline.",
                "OK");

            return true;
        }


    }

}
