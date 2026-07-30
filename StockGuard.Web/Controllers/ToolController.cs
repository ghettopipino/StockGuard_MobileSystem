using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;
using StockGuard.Web.Models;

namespace StockGuard.Web.Controllers
{
    public class ToolController : Controller
    {
        private readonly FirebaseService _firebase;
        private readonly QrCodeService _qrCode;

        private const int PageSize = 25;

        public ToolController(
            FirebaseService firebase,
            QrCodeService qrCode)
        {
            _firebase = firebase;
            _qrCode = qrCode;
        }

        private bool IsLoggedIn =>
            HttpContext.Session.GetString("UserEmail") != null;

        // ── Index — all tools with filters + pagination ───────────────────────
        public async Task<IActionResult> Index(
            string? catalogId = null,
            string? status = null,
            string? search = null,
            int page = 1)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allTools = await _firebase.GetAllToolsAsync();
            var catalogs = await _firebase.GetAllCatalogsAsync();

            // ── Stats — always from the full unfiltered set ───────────────────
            ViewBag.TotalTools = allTools.Count;
            ViewBag.AvailableTools = allTools.Count(t => t.Status == "Available");
            ViewBag.BorrowedTools = allTools.Count(t => t.Status == "Borrowed");

            // ── Apply filters ─────────────────────────────────────────────────
            var filtered = allTools.AsEnumerable();

            if (!string.IsNullOrEmpty(catalogId))
                filtered = filtered.Where(t => t.CatalogId == catalogId);

            if (!string.IsNullOrEmpty(status))
                filtered = filtered.Where(t => t.Status == status);

            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(t =>
                    t.ToolId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    t.ToolName.Contains(search, StringComparison.OrdinalIgnoreCase));

            var filteredList = filtered
                .OrderBy(t => t.ToolName)
                .ThenBy(t => t.ToolId)
                .ToList();

            // ── Pagination ────────────────────────────────────────────────────
            int totalFiltered = filteredList.Count;
            int totalPages = totalFiltered == 0
                ? 1
                : (int)Math.Ceiling((double)totalFiltered / PageSize);

            page = Math.Max(1, Math.Min(page, totalPages));

            var paged = filteredList
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // ── ViewBag ───────────────────────────────────────────────────────
            ViewBag.Tools = paged;
            ViewBag.Catalogs = catalogs;
            ViewBag.SelectedCatalog = catalogId;
            ViewBag.SelectedStatus = status;
            ViewBag.SearchText = search;

            // Pagination metadata
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalFiltered = totalFiltered;
            ViewBag.PageSize = PageSize;
            ViewBag.FirstItem = totalFiltered == 0 ? 0 : (page - 1) * PageSize + 1;
            ViewBag.LastItem = Math.Min(page * PageSize, totalFiltered);
            ViewBag.HasPrevPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            // 5-page window centred on current page
            int windowStart = Math.Max(1, page - 2);
            int windowEnd = Math.Min(totalPages, windowStart + 4);
            windowStart = Math.Max(1, windowEnd - 4);
            ViewBag.PageRange = Enumerable.Range(
                windowStart, windowEnd - windowStart + 1).ToList();

            return View();
        }

        // ── QR Code page ──────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> QrCode(string toolId)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(toolId))
                return RedirectToAction("Index");

            var allTools = await _firebase.GetAllToolsAsync();
            var tool = allTools.FirstOrDefault(t => t.ToolId == toolId);

            if (tool == null)
            {
                TempData["Error"] = $"Tool '{toolId}' not found.";
                return RedirectToAction("Index");
            }

            var catalogs = await _firebase.GetAllCatalogsAsync();
            var catalog = catalogs.FirstOrDefault(c => c.CatalogId == tool.CatalogId);

            string qrBase64 = string.Empty;
            try { qrBase64 = _qrCode.GenerateQrCodeBase64(tool.ToolId); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"QR error: {ex.Message}");
            }

            ViewBag.Tool = tool;
            ViewBag.CatalogName = catalog?.CatalogName ?? "Unknown";
            ViewBag.QrCodeBase64 = qrBase64;

            return View();
        }

        // ── Print all QR codes for a catalog ─────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> PrintAll(string catalogId)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allTools = await _firebase.GetAllToolsAsync();
            var tools = allTools
                .Where(t => t.CatalogId == catalogId)
                .OrderBy(t => t.ToolId)
                .ToList();

            var catalogs = await _firebase.GetAllCatalogsAsync();
            var catalog = catalogs.FirstOrDefault(c => c.CatalogId == catalogId);

            var toolsWithQr = new List<object>();
            foreach (var t in tools)
            {
                var qrBase64 = string.Empty;
                try { qrBase64 = _qrCode.GenerateQrCodeBase64(t.ToolId); } catch { }
                toolsWithQr.Add(new { Tool = t, QrCodeBase64 = qrBase64 });
            }

            ViewBag.ToolsWithQr = toolsWithQr;
            ViewBag.CatalogName = catalog?.CatalogName ?? "Unknown";
            ViewBag.CatalogId = catalogId;

            return View();
        }

        // ── Download QR ───────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DownloadQr(string toolId)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var bytes = _qrCode.GenerateQrCodeBytes(toolId);
            if (bytes == null || bytes.Length == 0)
                return RedirectToAction("Index");

            return File(bytes, "image/png", $"QR-{toolId}.png");
        }

        // ── Test QR ───────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult TestQr()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var testBase64 = string.Empty;
            var errorMsg = string.Empty;

            try { testBase64 = _qrCode.GenerateQrCodeBase64("TEST-001"); }
            catch (Exception ex) { errorMsg = ex.Message; }

            return Content(
                string.IsNullOrEmpty(testBase64)
                    ? $"QR FAILED: {errorMsg}"
                    : $"QR SUCCESS: base64 length = {testBase64.Length} chars",
                "text/plain");
        }
    }
}