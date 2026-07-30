using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;

namespace StockGuard.Web.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly FirebaseService _firebase;

        public AnalyticsController(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        private bool IsLoggedIn =>
            HttpContext.Session.GetString("UserEmail") != null;

        public async Task<IActionResult> Index(string? projectId = null)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allProjects = await _firebase.GetAllProjectsAsync();
            var allTools = await _firebase.GetAllToolsAsync();
            var allUsers = await _firebase.GetAllUsersAsync();
            var allTransactions = await _firebase.GetAllTransactionsAsync();
            var allReports = await _firebase.GetAllDamageReportsAsync();

            // ── Select project ────────────────────────────────────────
            StockGuard.Web.Models.Project? selectedProject;

            if (!string.IsNullOrEmpty(projectId))
                selectedProject = allProjects
                    .FirstOrDefault(p => p.ProjectId == projectId);
            else
                selectedProject = allProjects
                    .FirstOrDefault(p => p.Status == "Completed") ??
                    allProjects.FirstOrDefault();

            ViewBag.Projects = allProjects;
            ViewBag.SelectedProject = selectedProject;

            if (selectedProject == null)
                return View();

            // ── Tools assigned to this project ────────────────────────
            // Tool has ProjectId — use it as the primary project filter
            var projectTools = allTools
                .Where(t => t.ProjectId == selectedProject.ProjectId &&
                            !t.IsDeleted)
                .ToList();

            var projectToolIds = projectTools
                .Select(t => t.ToolId)
                .ToHashSet();

            // ── Transactions scoped to this project's tools ───────────
            // TransactionLog has no ProjectId — match via ToolId
            var projectTransactions = allTransactions
                .Where(t => projectToolIds.Contains(t.ToolId))
                .ToList();

            // ── Damage reports scoped to this project's tools ─────────
            // DamageReport has no ProjectId — match via ToolId
            var projectReports = allReports
                .Where(r => projectToolIds.Contains(r.ToolId))
                .ToList();

            // ── Worker IDs active on this project ─────────────────────
            var deployedWorkerIds = projectTransactions
                .Select(t => t.WorkerId)
                .Distinct()
                .ToHashSet();

            // ── Tool stats (project-scoped) ───────────────────────────
            ViewBag.TotalTools = projectTools.Count;
            ViewBag.AvailableTools = projectTools.Count(t => t.Status == "Available");
            ViewBag.DamagedTools = projectTools.Count(t => t.Status == "Damaged" ||
                                                             t.Status == "UnderRepair");
            ViewBag.LostTools = projectTools.Count(t => t.Status == "Lost");

            // ── Transaction stats (project-scoped) ───────────────────
            ViewBag.TotalTransactions = projectTransactions.Count;
            ViewBag.TotalBorrows = projectTransactions.Count(t => t.Action == "Borrowed");
            ViewBag.TotalReturns = projectTransactions.Count(t => t.Action == "Returned");
            ViewBag.TotalTransfers = projectTransactions.Count(t => t.Action == "Transferred");

            // ── Worker stats (project-scoped) ─────────────────────────
            var projectWorkers = allUsers
                .Where(u => u.Role == "Worker" &&
                            u.AccountStatus == "Approved" &&
                            deployedWorkerIds.Contains(u.UniqueKey))
                .ToList();

            var workerStats = projectWorkers.Select(w => new
            {
                Worker = w,
                Borrows = projectTransactions.Count(t => t.WorkerId == w.UniqueKey &&
                                                         t.Action == "Borrowed"),
                Damages = projectReports.Count(r => r.WorkerId == w.UniqueKey)
            })
            .OrderByDescending(w => w.Borrows)
            .ToList();

            ViewBag.WorkerStats = workerStats;
            ViewBag.MostActiveWorker = workerStats.FirstOrDefault();
            ViewBag.MostDamageWorker = workerStats
                .OrderByDescending(w => w.Damages)
                .FirstOrDefault();

            // ── Tool usage stats (project-scoped) ────────────────────
            var toolUsage = projectTools.Select(t => new
            {
                Tool = t,
                Usage = projectTransactions.Count(tx => tx.ToolId == t.ToolId &&
                                                          tx.Action == "Borrowed"),
                Damages = projectReports.Count(r => r.ToolId == t.ToolId)
            })
            .OrderByDescending(t => t.Usage)
            .ToList();

            ViewBag.ToolUsage = toolUsage;
            ViewBag.MostUsedTool = toolUsage.FirstOrDefault();
            ViewBag.MostDamagedTool = toolUsage
                .OrderByDescending(t => t.Damages)
                .FirstOrDefault();

            // ── Damage report stats (project-scoped) ──────────────────
            ViewBag.TotalReports = projectReports.Count;
            ViewBag.PendingReports = projectReports.Count(r => r.Status == "Pending");
            ViewBag.ResolvedReports = projectReports.Count(r => r.Status == "Resolved");

            // ── Deployed Workers ──────────────────────────────────────
            ViewBag.DeployedWorkers = allUsers
                .Where(u => deployedWorkerIds.Contains(u.UniqueKey) &&
                            !u.IsDeleted)
                .Select(u => new
                {
                    u.FullName,
                    u.Email,
                    u.Role,
                    IsActive = u.AccountStatus == "Approved"
                })
                .OrderBy(u => u.FullName)
                .ToList();

            // ── Deployed Tools ────────────────────────────────────────
            ViewBag.DeployedTools = projectTools
                .Select(t => new
                {
                    t.ToolName,
                    t.ToolId,
                    t.Status,
                    t.StatusBadgeClass
                })
                .OrderBy(t => t.ToolName)
                .ToList();

            return View();
        }
    }
}