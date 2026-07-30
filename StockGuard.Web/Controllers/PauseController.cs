using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;
using StockGuard.Web.Models;

namespace StockGuard.Web.Controllers
{
    public class PauseController : Controller
    {
        private readonly FirebaseService _firebase;

        public PauseController(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        private bool IsLoggedIn =>
            HttpContext.Session.GetString("UserEmail") != null;

        private string CurrentUserName =>
            HttpContext.Session.GetString("UserName")
                ?? string.Empty;

        // ── INDEX ─────────────────────────────────────────────────
        // KEY FIX: use GetAllPauseRequestsRawAsync() so we get the
        // actual Firebase key, then assign it to RequestId before
        // passing to the view. This guarantees the key in the form
        // matches what the controller looks up.
        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allRaw = await _firebase
                .GetAllPauseRequestsRawAsync();

            // Stamp each report's RequestId with the Firebase key
            var all = allRaw
                .Where(r => r.Report != null)
                .Select(r =>
                {
                    r.Report!.RequestId = r.Key; // ← THE FIX
                    return r.Report;
                })
                .ToList();

            ViewBag.PendingRequests = all
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.RequestDate)
                .ToList();

            ViewBag.ProcessedRequests = all
                .Where(r => r.Status != "Pending")
                .OrderByDescending(r => r.RequestDate)
                .Take(20)
                .ToList();

            ViewBag.PendingCount = all.Count(r =>
                r.Status == "Pending");
            ViewBag.ApprovedCount = all.Count(r =>
                r.Status == "Approved");

            return View();
        }

        // ── APPROVE (single) ──────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Approve(
            string requestKey, string toolId)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var success = await ApproveOne(requestKey, toolId);

            TempData[success ? "Success" : "Error"] = success
                ? "Pause approved. Tool marked as On Hold."
                : $"Could not find pause request. Key: {requestKey}";

            return RedirectToAction("Index");
        }

        // ── REJECT (single) ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Reject(
            string requestKey, string toolId)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            // requestKey is now always the Firebase key
            var allRaw = await _firebase
                .GetAllPauseRequestsRawAsync();

            var match = allRaw.FirstOrDefault(r =>
                r.Key == requestKey);

            if (match?.Report != null)
            {
                match.Report.Status = "Rejected";

                await _firebase.UpdatePauseRequestAsync(
                    match.Key, match.Report);

                // Reset tool back to Borrowed
                var id = string.IsNullOrEmpty(toolId)
                    ? match.Report.ToolId
                    : toolId;

                var tool = await _firebase.GetToolByIdAsync(id);
                if (tool != null)
                {
                    tool.Status = "Borrowed";
                    await _firebase.UpdateToolAsync(tool);
                }

                TempData["Error"] =
                    "Pause request rejected.";
            }
            else
            {
                TempData["Error"] =
                    $"Could not find request. Key: {requestKey}";
            }

            return RedirectToAction("Index");
        }

        // ── APPROVE ALL ───────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ApproveAll()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allRaw = await _firebase
                .GetAllPauseRequestsRawAsync();

            var pending = allRaw
                .Where(r => r.Report?.Status == "Pending")
                .ToList();

            if (pending.Count == 0)
            {
                TempData["Error"] =
                    "No pending requests to approve.";
                return RedirectToAction("Index");
            }

            int count = 0;
            foreach (var item in pending)
            {
                if (item.Report is null) continue;
                // Pass Firebase key directly
                var ok = await ApproveOne(
                    item.Key, item.Report.ToolId);
                if (ok) count++;
            }

            TempData["Success"] =
                $"Approved {count} of {pending.Count} " +
                $"pause requests.";

            return RedirectToAction("Index");
        }

        // ── APPROVE SELECTED ──────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ApproveSelected(
            List<string> selectedKeys,
            List<string> selectedToolIds)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            if (selectedKeys == null ||
                selectedKeys.Count == 0)
            {
                TempData["Error"] =
                    "No requests selected.";
                return RedirectToAction("Index");
            }

            int count = 0;
            for (int i = 0; i < selectedKeys.Count; i++)
            {
                var key = selectedKeys[i];
                var toolId = i < selectedToolIds?.Count
                    ? selectedToolIds[i]
                    : string.Empty;

                // selectedKeys already contain Firebase keys
                // because Index stamped them above
                var ok = await ApproveOne(key, toolId);
                if (ok) count++;
            }

            TempData["Success"] =
                $"Approved {count} selected pause request(s).";

            return RedirectToAction("Index");
        }

        // ── SHARED APPROVE HELPER ─────────────────────────────────
        // requestKey is always the Firebase key now
        private async Task<bool> ApproveOne(
            string requestKey, string toolId)
        {
            var allRaw = await _firebase
                .GetAllPauseRequestsRawAsync();

            // Direct key match — no ambiguity
            var match = allRaw.FirstOrDefault(r =>
                r.Key == requestKey);

            if (match?.Report == null) return false;

            match.Report.Status = "Approved";
            match.Report.ApprovedDate = DateTime.Now;
            match.Report.ApprovedBy = CurrentUserName;

            await _firebase.UpdatePauseRequestAsync(
                match.Key, match.Report);

            var id = string.IsNullOrEmpty(toolId)
                ? match.Report.ToolId
                : toolId;

            var tool = await _firebase.GetToolByIdAsync(id);
            if (tool != null)
            {
                tool.Status = "OnHold";
                await _firebase.UpdateToolAsync(tool);
            }

            return true;
        }
    }
}
