using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;

namespace StockGuard.Web.Controllers
{
    public class WorkerController : Controller
    {
        private readonly FirebaseService _firebase;

        public WorkerController(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        private bool IsLoggedIn =>
            HttpContext.Session
                .GetString("UserEmail") != null;

        // ── Index — all workers ───────────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allUsers =
                await _firebase.GetAllUsersAsync();
            var allTools =
                await _firebase.GetAllToolsAsync();

            var workers = allUsers
                .Where(u => u.Role == "Worker")
                .OrderBy(u => u.FullName)
                .ToList();

            // Add tool count per worker
            var workerDisplay = workers.Select(w =>
            {
                var assignedTools = allTools
                    .Count(t => t.AssignedWorkerId ==
                                w.UniqueKey);
                return new
                {
                    Worker = w,
                    AssignedTools = assignedTools,
                    ActivityStatus = assignedTools > 0
                        ? "Active"
                        : "Idle"
                };
            }).ToList();

            ViewBag.WorkerDisplay = workerDisplay;
            ViewBag.TotalWorkers = workers
                .Count(w => w.AccountStatus == "Approved");
            ViewBag.PendingWorkers = workers
                .Count(w => w.AccountStatus == "Pending");
            ViewBag.ActiveWorkers = workerDisplay
                .Count(w => w.ActivityStatus == "Active");

            return View();
        }

        // ── Approve worker ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Approve(
            string workerKey)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allUsers =
                await _firebase.GetAllUsersAsync();

            var worker = allUsers.FirstOrDefault(
                u => u.UniqueKey == workerKey);

            if (worker != null)
            {
                worker.AccountStatus = "Approved";
                await _firebase.UpdateUserAsync(worker);
                TempData["Success"] =
                    $"{worker.FullName} has been approved.";
            }

            return RedirectToAction("Index");
        }

        // ── Reject worker ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Reject(
            string workerKey)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allUsers =
                await _firebase.GetAllUsersAsync();

            var worker = allUsers.FirstOrDefault(
                u => u.UniqueKey == workerKey);

            if (worker != null)
            {
                worker.AccountStatus = "Rejected";
                await _firebase.UpdateUserAsync(worker);
                TempData["Success"] =
                    $"{worker.FullName} has been rejected.";
            }

            return RedirectToAction("Index");
        }

        // ── Remove worker ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Remove(
            string workerKey)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allUsers =
                await _firebase.GetAllUsersAsync();
            var allTools =
                await _firebase.GetAllToolsAsync();

            var worker = allUsers.FirstOrDefault(
                u => u.UniqueKey == workerKey);

            if (worker != null)
            {
                // Check if worker has tools
                var hasTools = allTools.Any(
                    t => t.AssignedWorkerId == workerKey);

                if (hasTools)
                {
                    TempData["Error"] =
                        $"Cannot remove {worker.FullName}. " +
                        $"Worker still has assigned tools.";
                    return RedirectToAction("Index");
                }

                worker.IsDeleted = true;
                await _firebase.UpdateUserAsync(worker);
                TempData["Success"] =
                    $"{worker.FullName} has been removed.";
            }

            return RedirectToAction("Index");
        }
    }
}