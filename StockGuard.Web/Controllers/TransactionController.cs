using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;
using StockGuard.Web.Models;

namespace StockGuard.Web.Controllers
{
    public class TransactionController : Controller
    {
        private readonly FirebaseService _firebase;

        // Records shown per page. Change here to affect both the
        // controller and the view's "Showing X–Y of Z" label.
        private const int PageSize = 25;

        public TransactionController(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        private bool IsLoggedIn =>
            HttpContext.Session.GetString("UserEmail") != null;

        // ── Index ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(
            string? filter = null,
            string? search = null,
            int page = 1)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            // Load the full dataset once
            var all = await _firebase.GetAllTransactionsAsync();

            // ── Stats — always from the full unfiltered set ───────────────────
            // Users expect the stat cards to reflect totals across all records,
            // not just the current page or current filter.
            var model = new TransactionViewModel
            {
                TotalCount = all.Count,
                BorrowCount = all.Count(t => t.Action == "Borrowed"),
                ReturnCount = all.Count(t => t.Action == "Returned"),
                DamageCount = all.Count(t => t.Action == "Damaged"),
                TransferCount = all.Count(t => t.Action == "Transferred"),
                SelectedAction = filter,
                SearchText = search,
                PageSize = PageSize,
            };

            // ── Apply filters to get the full filtered set ────────────────────
            var filtered = all.AsEnumerable();

            if (!string.IsNullOrEmpty(filter))
                filtered = filtered
                    .Where(t => t.Action == filter);

            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(t =>
                    (t.ToolName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.WorkerName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.ToolId ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));

            var filteredList = filtered
                .OrderByDescending(t => t.Date)
                .ToList();

            // ── Pagination ────────────────────────────────────────────────────
            model.TotalFiltered = filteredList.Count;

            // Clamp page to valid range (handles stale bookmarks/back-button)
            model.CurrentPage = Math.Max(1, Math.Min(page, model.TotalPages));

            model.Transactions = filteredList
                .Skip((model.CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return View(model);
        }

        // ── API endpoint used by the real-time JS updater ─────────────────────
        // Returns the first page of fresh data so the live-update script can
        // refresh the table without a full page reload.
        [HttpGet("/api/transactions-full")]
        public async Task<IActionResult> TransactionsFull(
            string? search = null,
            string? filter = null)
        {
            if (!IsLoggedIn)
                return Unauthorized();

            var all = await _firebase.GetAllTransactionsAsync();

            var filtered = all.AsEnumerable();

            if (!string.IsNullOrEmpty(filter))
                filtered = filtered.Where(t => t.Action == filter);

            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(t =>
                    (t.ToolName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.WorkerName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.ToolId ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));

            var list = filtered
                .OrderByDescending(t => t.Date)
                .Take(PageSize) // real-time update only refreshes the first page
                .ToList();

            return Json(new
            {
                totalCount = all.Count,
                borrowCount = all.Count(t => t.Action == "Borrowed"),
                returnCount = all.Count(t => t.Action == "Returned"),
                damageCount = all.Count(t => t.Action == "Damaged"),
                items = list.Select(tx => new
                {
                    toolName = tx.ToolName,
                    toolId = tx.ToolId,
                    workerName = tx.WorkerName,
                    action = tx.Action,
                    actionBadgeClass = tx.ActionBadgeClass,
                    description = tx.Description,
                    condition = tx.Condition,
                    date = tx.DateLabel
                })
            });
        }

        // ── Debug ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Debug()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var transactions = await _firebase.GetAllTransactionsAsync();
            var result = $"Count: {transactions.Count}\n\n";

            foreach (var tx in transactions.Take(5))
                result +=
                    $"Tool: {tx.ToolName} ({tx.ToolId})\n" +
                    $"Worker: {tx.WorkerName}\n" +
                    $"Action: {tx.Action}\n" +
                    $"Date: {tx.Date}\n\n";

            return Content(result, "text/plain");
        }
    }
}