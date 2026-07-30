using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;
using StockGuard.Web.Models;

namespace StockGuard.Web.Controllers
{
    public class EquipmentController : Controller
    {
        private readonly FirebaseService _firebase;
        private readonly QrCodeService _qrCode;

        public EquipmentController(
            FirebaseService firebase,
            QrCodeService qrCode)
        {
            _firebase = firebase;
            _qrCode = qrCode;
        }

        private bool IsLoggedIn =>
            HttpContext.Session
                .GetString("UserEmail") != null;

        // ── Index — show all catalogs ─────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var catalogs =
                await _firebase.GetAllCatalogsAsync();
            var allTools =
                await _firebase.GetAllToolsAsync();

            // Build catalog display with tool stats
            var catalogDisplay = catalogs.Select(c =>
            {
                var tools = allTools
                    .Where(t => t.CatalogId == c.CatalogId)
                    .ToList();
                return new
                {
                    Catalog = c,
                    TotalTools = tools.Count,
                    Available = tools
                        .Count(t => t.Status == "Available"),
                    Borrowed = tools
                        .Count(t => t.Status == "Borrowed"),
                    Damaged = tools
                        .Count(t => t.Status == "Damaged" ||
                                    t.Status == "UnderRepair")
                };
            }).ToList();

            ViewBag.CatalogDisplay = catalogDisplay;
            ViewBag.TotalCatalogs = catalogs.Count;
            ViewBag.TotalTools = allTools.Count;
            ViewBag.AvailableTools = allTools
                .Count(t => t.Status == "Available");

            return View();
        }

        // ── Create catalog ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create(
            string catalogName,
            string prefix,
            int quantity,
            string description)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(catalogName) ||
                string.IsNullOrWhiteSpace(prefix) ||
                quantity <= 0)
            {
                TempData["Error"] =
                    "Please fill in all required fields.";
                return RedirectToAction("Index");
            }

            var catalogId =
                $"CAT-{prefix.ToUpper().Trim()}" +
                $"-{DateTime.Now:yyyyMMddHHmmss}";

            var catalog = new EquipmentCatalog
            {
                CatalogId = catalogId,
                CatalogName = catalogName.Trim(),
                Prefix = prefix.ToUpper().Trim(),
                Quantity = quantity,
                Description = description?.Trim()
                              ?? string.Empty,
                DateCreated = DateTime.Now,
                IsDeleted = false
            };

            await _firebase.CreateCatalogAsync(catalog);

            // Generate individual tools
            for (int i = 1; i <= quantity; i++)
            {
                var toolId =
                    $"{prefix.ToUpper().Trim()}" +
                    $"-{i.ToString().PadLeft(3, '0')}";

                var existing = await _firebase
                    .GetToolByIdAsync(toolId);

                if (existing != null)
                    toolId = $"{prefix.ToUpper().Trim()}" +
                             $"-{DateTime.Now.Ticks % 10000}" +
                             $"-{i.ToString().PadLeft(3, '0')}";

                var tool = new Tool
                {
                    ToolId = toolId,
                    ToolName = catalogName.Trim(),
                    CatalogId = catalogId,
                    Status = "Available",
                    QrCode = toolId,
                    Condition = "Good",
                    IsDeleted = false
                };

                await _firebase.CreateToolAsync(tool);
            }

            TempData["Success"] =
                $"{catalogName} catalog created with " +
                $"{quantity} tools.";

            return RedirectToAction("Index");
        }

        // ── Delete catalog ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(
            string catalogId)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            var allTools =
                await _firebase.GetAllToolsAsync();

            var catalogTools = allTools
                .Where(t => t.CatalogId == catalogId)
                .ToList();

            // Check for active tools
            var activeTools = catalogTools
                .Where(t => t.Status == "Borrowed" ||
                            t.Status == "Damaged")
                .ToList();

            if (activeTools.Count > 0)
            {
                TempData["Error"] =
                    $"Cannot delete catalog. " +
                    $"{activeTools.Count} tool(s) are " +
                    $"currently in use.";
                return RedirectToAction("Index");
            }

            // Soft delete all tools
            foreach (var tool in catalogTools)
            {
                tool.IsDeleted = true;
                await _firebase.UpdateToolAsync(tool);
            }

            await _firebase.DeleteCatalogAsync(catalogId);

            TempData["Success"] =
                "Catalog deleted successfully.";

            return RedirectToAction("Index");
        }
        // ── Add tools to an existing catalog ─────────────────────────────
        [HttpPost]
        public async Task<IActionResult> AddTools(
            string catalogId,
            List<string> toolNames,
            List<string> conditions)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(catalogId) ||
                toolNames == null || toolNames.Count == 0)
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction("Index");
            }

            var catalogs = await _firebase.GetAllCatalogsAsync();
            var catalog = catalogs.FirstOrDefault(
                c => c.CatalogId == catalogId);

            if (catalog == null)
            {
                TempData["Error"] = "Catalog not found.";
                return RedirectToAction("Index");
            }

            // Find highest existing tool number for this prefix
            var allTools = await _firebase.GetAllToolsAsync();
            var existingNumbers = allTools
                .Where(t => t.CatalogId == catalogId)
                .Select(t =>
                {
                    var parts = t.ToolId.Split('-');
                    return parts.Length >= 2 &&
                           int.TryParse(parts.Last(), out int n)
                           ? n : 0;
                })
                .ToList();

            int nextNumber = existingNumbers.Count > 0
                ? existingNumbers.Max() + 1
                : catalog.Quantity + 1;

            int added = 0;
            for (int i = 0; i < toolNames.Count; i++)
            {
                var rawName = toolNames[i]?.Trim();
                if (string.IsNullOrWhiteSpace(rawName)) continue;

                var toolId =
                    $"{catalog.Prefix}" +
                    $"-{nextNumber.ToString().PadLeft(3, '0')}";

                // Guarantee uniqueness
                var existing =
                    await _firebase.GetToolByIdAsync(toolId);
                if (existing != null)
                    toolId =
                        $"{catalog.Prefix}" +
                        $"-{DateTime.Now.Ticks % 10000}" +
                        $"-{nextNumber.ToString().PadLeft(3, '0')}";

                nextNumber++;

                var condition =
                    (conditions != null && i < conditions.Count)
                    ? conditions[i]
                    : "Good";

                var tool = new Tool
                {
                    ToolId = toolId,
                    ToolName = rawName,
                    CatalogId = catalogId,
                    Status = "Available",
                    QrCode = toolId,
                    Condition = condition,
                    IsDeleted = false
                };

                await _firebase.CreateToolAsync(tool);
                added++;
            }

            // Sync catalog quantity
            catalog.Quantity += added;
            await _firebase.UpdateCatalogAsync(catalog);

            TempData["Success"] =
                $"{added} tool(s) added to {catalog.CatalogName}.";
            return RedirectToAction("Index");
        }
    }
}