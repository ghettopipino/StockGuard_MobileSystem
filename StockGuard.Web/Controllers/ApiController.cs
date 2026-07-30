using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;
using StockGuard.Web.Models;

namespace StockGuard.Web.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly FirebaseService _firebase;

        public ApiController(
            FirebaseService firebase)
        {
            _firebase = firebase;
        }

        private bool IsLoggedIn =>
            HttpContext.Session
                .GetString("UserEmail") != null;

        // ── Dashboard Stats ───────────────────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            if (!IsLoggedIn)
                return Unauthorized();

            var allTools =
                await _firebase.GetAllToolsAsync();
            var allUsers =
                await _firebase.GetAllUsersAsync();
            var allReports =
                await _firebase
                    .GetAllDamageReportsAsync();
            var activeProject =
                await _firebase.GetActiveProjectAsync();

            return Ok(new
            {
                totalTools = allTools.Count,
                availableTools = allTools
                    .Count(t => t.Status == "Available"),
                borrowedTools = allTools
                    .Count(t => t.Status == "Borrowed"),
                damagedTools = allTools
                    .Count(t => t.Status == "Damaged" ||
                                t.Status == "UnderRepair"),
                totalWorkers = allUsers
                    .Count(u => u.Role == "Worker" &&
                                u.AccountStatus
                                == "Approved"),
                pendingWorkers = allUsers
                    .Count(u => u.Role == "Worker" &&
                                u.AccountStatus
                                == "Pending"),
                pendingReports = allReports
                    .Count(r => r.Status == "Pending"),
                activeProject = activeProject == null
                    ? null
                    : new
                    {
                        name =
                            activeProject.ProjectName,
                        location =
                            activeProject.Location,
                        status =
                            activeProject.Status
                    }
            });
        }

        // ── Borrowed Tools ────────────────────────────────────────
        [HttpGet("borrowed-tools")]
        public async Task<IActionResult>
            GetBorrowedTools()
        {
            if (!IsLoggedIn)
                return Unauthorized();

            var allTools =
                await _firebase.GetAllToolsAsync();

            var borrowed = allTools
                .Where(t => t.Status == "Borrowed")
                .OrderByDescending(t => t.BorrowDate)
                .Take(5)
                .Select(t => new
                {
                    toolId =
                        t.ToolId,
                    toolName =
                        t.ToolName,
                    status =
                        t.Status,
                    assignedWorkerName =
                        t.AssignedWorkerName,
                    statusBadgeClass =
                        t.StatusBadgeClass
                })
                .ToList();

            return Ok(borrowed);
        }

        // ── Damage Reports ────────────────────────────────────────
        [HttpGet("damage-reports")]
        public async Task<IActionResult>
            GetDamageReports()
        {
            if (!IsLoggedIn)
                return Unauthorized();

            var reports =
                await _firebase
                    .GetAllDamageReportsAsync();

            var pending = reports
                .Where(r => r.Status == "Pending")
                .Take(5)
                .Select(r => new
                {
                    toolId = r.ToolId,
                    toolName = r.ToolName,
                    workerName = r.WorkerName,
                    severity = r.Severity,
                    status = r.Status,
                    severityBadgeClass =
                        r.SeverityBadgeClass
                })
                .ToList();

            return Ok(pending);
        }

        // ── Recent Transactions ───────────────────────────────────
        [HttpGet("transactions")]
        public async Task<IActionResult>
            GetTransactions()
        {
            if (!IsLoggedIn)
                return Unauthorized();

            var transactions =
                await _firebase
                    .GetAllTransactionsAsync();

            var recent = transactions
                .Take(10)
                .Select(t => new
                {
                    toolId = t.ToolId,
                    toolName = t.ToolName,
                    workerName = t.WorkerName,
                    action = t.Action,
                    description = t.Description,
                    condition = t.Condition,
                    date = t.DateLabel,
                    actionBadgeClass =
                        t.ActionBadgeClass
                })
                .ToList();

            return Ok(new
            {
                total = transactions.Count,
                borrowed = transactions
                    .Count(t => t.Action == "Borrowed"),
                returned = transactions
                    .Count(t => t.Action == "Returned"),
                damaged = transactions
                    .Count(t => t.Action == "Damaged"),
                items = recent
            });
        }

        // ── Workers ───────────────────────────────────────────────
        [HttpGet("workers")]
        public async Task<IActionResult> GetWorkers()
        {
            if (!IsLoggedIn)
                return Unauthorized();

            var allUsers =
                await _firebase.GetAllUsersAsync();
            var allTools =
                await _firebase.GetAllToolsAsync();

            var workers = allUsers
                .Where(u => u.Role == "Worker")
                .Select(w => new
                {
                    uniqueKey = w.UniqueKey,
                    fullName = w.FullName,
                    email = w.Email,
                    accountStatus = w.AccountStatus,
                    statusBadgeClass =
                        w.StatusBadgeClass,
                    assignedTools = allTools
                        .Count(t =>
                            t.AssignedWorkerId
                            == w.UniqueKey),
                    activityStatus = allTools
                        .Any(t =>
                            t.AssignedWorkerId
                            == w.UniqueKey)
                        ? "Active"
                        : "Idle"
                })
                .ToList();

            return Ok(new
            {
                total = workers
                    .Count(w =>
                        w.accountStatus == "Approved"),
                pending = workers
                    .Count(w =>
                        w.accountStatus == "Pending"),
                active = workers
                    .Count(w =>
                        w.activityStatus == "Active"),
                items = workers
            });
        }

        // ── Full Transactions with Filter ─────────────────────────
        [HttpGet("transactions-full")]
        public async Task<IActionResult>
            GetTransactionsFull(
                string? filter = null,
                string? search = null)
        {
            if (!IsLoggedIn)
                return Unauthorized();

            var all =
                await _firebase
                    .GetAllTransactionsAsync();

            var filtered = all.AsEnumerable();

            if (!string.IsNullOrEmpty(filter))
                filtered = filtered
                    .Where(t => t.Action == filter);

            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(t =>
                    (t.ToolName ?? "").Contains(
                        search,
                        StringComparison
                            .OrdinalIgnoreCase) ||
                    (t.WorkerName ?? "").Contains(
                        search,
                        StringComparison
                            .OrdinalIgnoreCase) ||
                    (t.ToolId ?? "").Contains(
                        search,
                        StringComparison
                            .OrdinalIgnoreCase));

            var list = filtered
                .OrderByDescending(t => t.Date)
                .ToList();

            return Ok(new
            {
                totalCount = all.Count,
                borrowCount = all.Count(
                    t => t.Action == "Borrowed"),
                returnCount = all.Count(
                    t => t.Action == "Returned"),
                damageCount = all.Count(
                    t => t.Action == "Damaged"),
                items = list.Select(t => new
                {
                    toolId = t.ToolId,
                    toolName = t.ToolName,
                    workerName = t.WorkerName,
                    action = t.Action,
                    description = t.Description,
                    condition = t.Condition,
                    date = t.DateLabel,
                    actionBadgeClass =
                        t.ActionBadgeClass
                }).ToList()
            });
        }
    }
}