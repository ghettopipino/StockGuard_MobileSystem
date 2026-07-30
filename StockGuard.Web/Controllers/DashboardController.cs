using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;
using StockGuard.Web.Models;

namespace StockGuard.Web.Controllers
{
    public class DashboardController : Controller
    {
        private readonly FirebaseService _firebase;

        public DashboardController(
            FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task<IActionResult> Index()
        {
            // ── Auth check ────────────────────────────────────────
            if (HttpContext.Session
                .GetString("UserEmail") == null)
                return RedirectToAction(
                    "Login", "Auth");

            // ── Load data ─────────────────────────────────────────
            var allTools =
                await _firebase.GetAllToolsAsync();
            var allUsers =
                await _firebase.GetAllUsersAsync();
            var allReports =
                await _firebase.GetAllDamageReportsAsync();
            var allTransactions =
                await _firebase.GetAllTransactionsAsync();
            var activeProject =
                await _firebase.GetActiveProjectAsync();
            var allProjects =
                await _firebase.GetAllProjectsAsync();

            // ── Stats ─────────────────────────────────────────────
            ViewBag.TotalTools = allTools.Count;
            ViewBag.AvailableTools = allTools
                .Count(t => t.Status == "Available");
            ViewBag.BorrowedTools = allTools
                .Count(t => t.Status == "Borrowed");
            ViewBag.DamagedTools = allTools
                .Count(t => t.Status == "Damaged" ||
                            t.Status == "UnderRepair");
            ViewBag.TotalWorkers = allUsers
                .Count(u => u.Role == "Worker" &&
                            u.AccountStatus == "Approved");
            ViewBag.PendingWorkers = allUsers
                .Count(u => u.Role == "Worker" &&
                            u.AccountStatus == "Pending");
            ViewBag.PendingReports = allReports
                .Count(r => r.Status == "Pending");
            ViewBag.TotalProjects = allProjects.Count;
            ViewBag.ActiveProject = activeProject;
            ViewBag.UserName =
                HttpContext.Session
                    .GetString("UserName");

            // ── Recent transactions ───────────────────────────────
            ViewBag.RecentTransactions = allTransactions
                .Take(10).ToList();

            // ── Recently borrowed tools ───────────────────────────
            ViewBag.BorrowedToolsList = allTools
                .Where(t => t.Status == "Borrowed")
                .OrderByDescending(t => t.BorrowDate)
                .Take(5).ToList();

            // ── Pending damage reports ────────────────────────────
            ViewBag.PendingReportsList = allReports
                .Where(r => r.Status == "Pending")
                .Take(5).ToList();

            return View();
        }
    }
}