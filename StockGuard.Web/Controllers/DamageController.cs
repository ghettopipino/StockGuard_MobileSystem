using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;
using StockGuard.Web.Models;

namespace StockGuard.Web.Controllers
{
    public class DamageController : Controller
    {
        private readonly FirebaseService _firebase;

        public DamageController(
            FirebaseService firebase)
        {
            _firebase = firebase;
        }

        private bool IsLoggedIn =>
            HttpContext.Session
                .GetString("UserEmail") != null;

        public async Task<IActionResult> Index(
            string? status = null)
        {
            if (!IsLoggedIn)
                return RedirectToAction(
                    "Login", "Auth");

            var rawReports = await _firebase
                .GetAllDamageReportsRawAsync();

            var filtered = rawReports.AsEnumerable();

            if (!string.IsNullOrEmpty(status))
                filtered = filtered
                    .Where(r => r.Report.Status
                                == status);

            ViewBag.Reports = filtered
                .OrderByDescending(r =>
                    r.Report.ReportDate)
                .ToList();

            ViewBag.TotalReports = rawReports.Count;
            ViewBag.PendingReports = rawReports
                .Count(r => r.Report.Status
                            == "Pending");
            ViewBag.ResolvedReports = rawReports
                .Count(r => r.Report.Status
                            == "Resolved");
            ViewBag.SelectedStatus = status;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Handle(
            string reportKey,
            string toolId,
            string action)
        {
            if (!IsLoggedIn)
                return RedirectToAction(
                    "Login", "Auth");

            var rawReports = await _firebase
                .GetAllDamageReportsRawAsync();

            var match = rawReports.FirstOrDefault(
                r => r.Key == reportKey);

            if (match != null)
            {
                match.Report.Status = action switch
                {
                    "resolve" => "Resolved",
                    "repair" => "UnderRepair",
                    "maintain" => "UnderRepair",
                    "lost" => "Lost",
                    _ => match.Report.Status
                };

                await _firebase
                    .UpdateDamageReportAsync(
                        match.Key, match.Report);

                var tool = await _firebase
                    .GetToolByIdAsync(toolId);

                if (tool != null)
                {
                    tool.Status = action switch
                    {
                        "resolve" => "Available",
                        "repair" => "UnderRepair",
                        "maintain" => "UnderRepair",
                        "lost" => "Lost",
                        _ => tool.Status
                    };

                    if (tool.Status == "Available" ||
                        tool.Status == "Lost")
                    {
                        tool.AssignedWorkerId =
                            string.Empty;
                        tool.AssignedWorkerName =
                            string.Empty;
                        tool.BorrowDate = null;
                    }

                    await _firebase
                        .UpdateToolAsync(tool);
                }

                TempData["Success"] =
                    "Report updated successfully.";
            }

            return RedirectToAction("Index");
        }
    }
}