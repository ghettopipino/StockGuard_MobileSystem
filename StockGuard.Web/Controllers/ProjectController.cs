using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;
using StockGuard.Web.Models;

namespace StockGuard.Web.Controllers
{
    public class ProjectController : Controller
    {
        private readonly FirebaseService _firebase;

        public ProjectController(FirebaseService firebase)
        {
            _firebase = firebase;
        }
        private static string? FindConflict(
    IList<Project> allProjects, string workerKey, string excludeProjectId) =>
    allProjects
        .FirstOrDefault(p =>
            p.ProjectId != excludeProjectId
            && p.Status == "Active"
            && (p.AssignedWorkerKeys ?? new List<string>()).Contains(workerKey))
        ?.ProjectName;

        private bool IsLoggedIn =>
            HttpContext.Session.GetString("UserEmail") != null;

        private string CurrentUserKey =>
            HttpContext.Session.GetString("UserKey") ?? string.Empty;

        private string CurrentUserName =>
            HttpContext.Session.GetString("UserName") ?? string.Empty;

        // ── Index ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var projects = await _firebase.GetAllProjectsAsync();

            ViewBag.Projects = projects;
            ViewBag.TotalProjects = projects.Count;
            ViewBag.ActiveProjects = projects.Count(p => p.Status == "Active");
            ViewBag.CompletedProjects = projects.Count(p => p.Status == "Completed");
            ViewBag.ActiveProject = projects.FirstOrDefault(p => p.Status == "Active");

            return View();
        }

        // ── Details ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(string id)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            var allProjects = await _firebase.GetAllProjectsAsync();
            var allTools = await _firebase.GetAllToolsAsync();
            var allUsers = await _firebase.GetAllUsersAsync();

            var project = allProjects.FirstOrDefault(p => p.ProjectId == id);
            if (project == null) return RedirectToAction("Index");

            // ── SOURCE OF TRUTH: read worker keys from projectWorkers node ──
            var assignedWorkerKeys = await _firebase.GetProjectWorkerKeysAsync(id);

            var assignedWorkers = allUsers
                .Where(u => assignedWorkerKeys.Contains(u.UniqueKey))
                .ToList();

            var deployedTools = allTools
                .Where(t => t.ProjectId == id)
                .ToList();

            // Get keys for the ONE active project that isn't this one
            // (there can only ever be one active project, but defensively check all)
            var occupiedKeys = new HashSet<string>();
            foreach (var other in allProjects.Where(p => p.ProjectId != id && p.Status == "Active"))
            {
                var keys = await _firebase.GetProjectWorkerKeysAsync(other.ProjectId);
                foreach (var k in keys) occupiedKeys.Add(k);
            }

            var availableWorkers = allUsers
    .Where(u => u.Role == "Worker"
             && u.AccountStatus == "Approved"
             && !u.IsDeleted
             && u.IsAvailable)
    .OrderBy(u => u.FullName)
    .ToList();

            var availableTools = allTools
                .Where(t => string.IsNullOrEmpty(t.ProjectId) && t.Status == "Available")
                .OrderBy(t => t.ToolName)
                .ToList();

            var catalogs = await _firebase.GetAllCatalogsAsync();

            ViewBag.Project = project;
            ViewBag.AssignedWorkers = assignedWorkers;
            ViewBag.DeployedTools = deployedTools;
            ViewBag.AvailableWorkers = availableWorkers;
            ViewBag.AvailableTools = availableTools;
            ViewBag.WorkerProjectMap = new Dictionary<string, string>(); // no longer needed
            ViewBag.AllCatalogs = catalogs;
            ViewBag.BorrowedCount = deployedTools.Count(t => !string.IsNullOrEmpty(t.AssignedWorkerId));

            return View();
        }

        // ── Create ────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create(
            string projectName,
            string location,
            string description)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(projectName) ||
                string.IsNullOrWhiteSpace(location))
            {
                TempData["Error"] = "Project name and location are required.";
                return RedirectToAction("Index");
            }

            if (projectName.Trim().Length < 2 || location.Trim().Length < 2)
            {
                TempData["Error"] =
                    "Project name and location must be at least 2 characters.";
                return RedirectToAction("Index");
            }

            var existing = await _firebase.GetAllProjectsAsync();
            foreach (var p in existing.Where(p => p.Status == "Active"))
            {
                p.Status = "Paused";
                await _firebase.UpdateProjectAsync(p);
            }

            var projectId = $"PRJ-{DateTime.Now:yyyyMMddHHmmss}";

            var project = new Project
            {
                ProjectId = projectId,
                ProjectName = projectName.Trim(),
                Location = location.Trim(),
                Description = description?.Trim() ?? string.Empty,
                StartDate = DateTime.Now,
                Status = "Active",
                CreatedBy = CurrentUserKey,
                CreatedByName = CurrentUserName,
                IsDeleted = false
            };

            await _firebase.CreateProjectAsync(project);

            TempData["Success"] = $"{projectName} created successfully.";
            return RedirectToAction("Details", new { id = projectId });
        }

        // ── Assign Worker ─────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> AssignWorker(string projectId, string workerKey)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            var allUsers = await _firebase.GetAllUsersAsync();
            var worker = allUsers.FirstOrDefault(u => u.UniqueKey == workerKey);

            if (worker == null)
            {
                TempData["Error"] = "Worker not found.";
                return RedirectToAction("Details", new { id = projectId });
            }

            if (!string.IsNullOrEmpty(worker.AssignedProjectId)
                && worker.AssignedProjectId != projectId)
            {
                var allProjects = await _firebase.GetAllProjectsAsync();
                var conflictProject = allProjects
                    .FirstOrDefault(p => p.ProjectId == worker.AssignedProjectId);
                TempData["Error"] =
                    $"Worker is already assigned to \"{conflictProject?.ProjectName ?? worker.AssignedProjectId}\".";
                return RedirectToAction("Details", new { id = projectId });
            }

            worker.AssignedProjectId = projectId;
            await _firebase.UpdateUserAsync(worker);
            await _firebase.AssignWorkerToProjectAsync(projectId, workerKey);

            TempData["Success"] = "Worker assigned successfully.";
            return RedirectToAction("Details", new { id = projectId });
        }

        // ── Remove Worker ─────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> RemoveWorker(string projectId, string workerKey)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            // Check if worker has any tools currently assigned
            var allTools = await _firebase.GetAllToolsAsync();
            var workerTools = allTools
                .Where(t => t.AssignedWorkerId == workerKey
                         && t.ProjectId == projectId)
                .ToList();

            if (workerTools.Count > 0)
            {
                var toolNames = string.Join(", ", workerTools.Select(t => t.ToolName));
                TempData["Error"] =
                    $"Cannot remove worker — they still have {workerTools.Count} tool(s) assigned: {toolNames}. Unassign the tools first.";
                return RedirectToAction("Details", new { id = projectId });
            }

            var allUsers = await _firebase.GetAllUsersAsync();
            var worker = allUsers.FirstOrDefault(u => u.UniqueKey == workerKey);

            if (worker != null)
            {
                worker.AssignedProjectId = string.Empty;
                await _firebase.UpdateUserAsync(worker);
            }

            await _firebase.RemoveWorkerFromProjectAsync(projectId, workerKey);

            TempData["Success"] = "Worker removed from project.";
            return RedirectToAction("Details", new { id = projectId });
        }

        // ── Deploy Tool ───────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> DeployTool(
            string projectId, string toolId)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            await _firebase.DeployToolToProjectAsync(projectId, toolId);

            var tool = await _firebase.GetToolByIdAsync(toolId);
            if (tool != null)
            {
                tool.ProjectId = projectId;
                await _firebase.UpdateToolAsync(tool);
            }

            TempData["Success"] = "Tool deployed to project.";
            return RedirectToAction("Details", new { id = projectId });
        }

        // ── Bulk Deploy Tools ─────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> BulkDeployTools(
            string projectId, string[] toolIds)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            if (toolIds == null || toolIds.Length == 0)
            {
                TempData["Error"] = "No tools selected.";
                return RedirectToAction("Details", new { id = projectId });
            }

            int successCount = 0;
            foreach (var toolId in toolIds)
            {
                await _firebase.DeployToolToProjectAsync(projectId, toolId);

                var tool = await _firebase.GetToolByIdAsync(toolId);
                if (tool != null)
                {
                    tool.ProjectId = projectId;
                    await _firebase.UpdateToolAsync(tool);
                    successCount++;
                }
            }

            TempData["Success"] = $"{successCount} tool(s) deployed to project.";
            return RedirectToAction("Details", new { id = projectId });
        }

        [HttpPost]
        public async Task<IActionResult> SetActive(string projectId)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            var projects = await _firebase.GetAllProjectsAsync();

            foreach (var p in projects.Where(p => p.Status == "Active"))
            {
                // Wipe workers from projectWorkers node before pausing
                var workerKeys = await _firebase.GetProjectWorkerKeysAsync(p.ProjectId);
                foreach (var key in workerKeys)
                    await _firebase.RemoveWorkerFromProjectAsync(p.ProjectId, key);

                p.Status = "Paused";
                await _firebase.UpdateProjectAsync(p);
            }

            var selected = projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (selected != null)
            {
                selected.Status = "Active";
                await _firebase.UpdateProjectAsync(selected);
            }

            TempData["Success"] = "Project set as active.";
            return RedirectToAction("Index");
        }

        // ── Complete Project ──────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Complete(string projectId)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            var project = await _firebase.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound();

            // Free all workers
            var allUsers = await _firebase.GetAllUsersAsync();
            foreach (var worker in allUsers
                .Where(u => u.AssignedProjectId == projectId))
            {
                worker.AssignedProjectId = string.Empty;
                await _firebase.UpdateUserAsync(worker);
            }

            // Free all tools
            var toolIds = await _firebase.GetProjectToolIdsAsync(projectId);
            foreach (var toolId in toolIds)
            {
                var tool = await _firebase.GetToolByIdAsync(toolId);
                if (tool != null && tool.Status == "Borrowed")
                {
                    tool.Status = "Available";
                    tool.AssignedWorkerId = string.Empty;
                    tool.AssignedWorkerName = string.Empty;
                    tool.ProjectId = string.Empty;
                    tool.BorrowDate = null;
                    await _firebase.UpdateToolAsync(tool);
                }
            }

            project.Status = "Completed";
            project.EndDate = DateTime.Now;
            await _firebase.UpdateProjectAsync(project);

            TempData["Success"] = $"{project.ProjectName} marked as completed.";
            return RedirectToAction("Index");
        }

        // ── Assign Tool to Worker ─────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> AssignTool(
            string projectId, string toolId, string workerKey)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allUsers = await _firebase.GetAllUsersAsync();
            var worker = allUsers.FirstOrDefault(u => u.UniqueKey == workerKey);

            if (worker == null)
            {
                TempData["Error"] = "Worker not found.";
                return RedirectToAction("Details", new { id = projectId });
            }

            var success = await _firebase.DirectAssignToolAsync(
                toolId, worker.UniqueKey, worker.FullName);

            TempData[success ? "Success" : "Error"] = success
                ? $"Tool assigned to {worker.FullName} successfully."
                : "Could not assign tool.";

            return RedirectToAction("Details", new { id = projectId });
        }

        // ── Bulk Assign Workers ───────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> BulkAssignWorkers(
    string projectId, string[] workerKeys)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            if (workerKeys == null || workerKeys.Length == 0)
            {
                TempData["Error"] = "No workers selected.";
                return RedirectToAction("Details", new { id = projectId });
            }

            var allUsers = await _firebase.GetAllUsersAsync();
            int successCount = 0;
            int skippedCount = 0;

            foreach (var workerKey in workerKeys)
            {
                var worker = allUsers.FirstOrDefault(u => u.UniqueKey == workerKey);
                if (worker == null) continue;

                // Skip if assigned elsewhere
                if (!string.IsNullOrEmpty(worker.AssignedProjectId)
                    && worker.AssignedProjectId != projectId)
                {
                    skippedCount++;
                    continue;
                }

                // Skip if already on this project
                if (worker.AssignedProjectId == projectId)
                {
                    skippedCount++;
                    continue;
                }

                worker.AssignedProjectId = projectId;
                await _firebase.UpdateUserAsync(worker);
                await _firebase.AssignWorkerToProjectAsync(projectId, workerKey);
                successCount++;
            }

            if (successCount > 0 && skippedCount == 0)
                TempData["Success"] = $"{successCount} worker(s) assigned successfully.";
            else if (successCount > 0)
                TempData["Success"] =
                    $"{successCount} assigned, {skippedCount} skipped.";
            else
                TempData["Error"] =
                    "All selected workers are already assigned elsewhere.";

            return RedirectToAction("Details", new { id = projectId });
        }

        // ── Bulk Assign Tools to Worker ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> BulkAssignTools(
            string projectId, string workerKey, string[] toolIds)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            if (toolIds == null || toolIds.Length == 0)
            {
                TempData["Error"] = "No tools selected.";
                return RedirectToAction("Details", new { id = projectId });
            }

            var allUsers = await _firebase.GetAllUsersAsync();
            var worker = allUsers.FirstOrDefault(u => u.UniqueKey == workerKey);

            if (worker == null)
            {
                TempData["Error"] = "Worker not found.";
                return RedirectToAction("Details", new { id = projectId });
            }

            int successCount = 0;
            foreach (var toolId in toolIds)
            {
                var success = await _firebase.DirectAssignToolAsync(
                    toolId, worker.UniqueKey, worker.FullName);
                if (success) successCount++;
            }

            TempData["Success"] =
                $"{successCount} tool(s) directly assigned to {worker.FullName}.";

            return RedirectToAction("Details", new { id = projectId });
        }

        // ── Unassign Tool from Worker ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> UnassignTool(
            string projectId, string toolId)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var success = await _firebase.UnassignToolAsync(toolId);

            TempData[success ? "Success" : "Error"] = success
                ? "Tool unassigned successfully."
                : "Could not unassign tool.";

            return RedirectToAction("Details", new { id = projectId });
        }
    }
}