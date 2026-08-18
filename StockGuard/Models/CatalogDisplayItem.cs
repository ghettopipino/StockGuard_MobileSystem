using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class CatalogDisplayItem
    {
        public EquipmentCatalog Catalog { get; }

        public string CatalogId =>
            Catalog.CatalogId;

        public string CatalogName =>
            Catalog.CatalogName;

        public string Prefix =>
            Catalog.Prefix;

        public string CatalogIcon =>
            Catalog.CatalogIcon;

        public string DateCreated =>
            Catalog.DateCreated.ToString("MMM d, yyyy");

        // ── Tool Counts ────────────────────────────────────────
        public int TotalTools { get; set; }

        public int AvailableTools { get; set; }

        public int BorrowedTools { get; set; }

        public int OnHoldTools { get; set; }

        public int DamagedTools { get; set; }

        public int LostTools { get; set; }

        // ── Labels ─────────────────────────────────────────────
        public string AvailableLabel =>
            $"{AvailableTools} available";

        public string BorrowedLabel =>
            $"{BorrowedTools} borrowed";

        public string OnHoldLabel =>
            $"{OnHoldTools} on hold";

        public string DamagedLabel =>
            $"{DamagedTools} damaged";

        public string LostLabel =>
            $"{LostTools} lost";

        public string TotalLabel =>
            TotalTools == 1
                ? "1 tool"
                : $"{TotalTools} tools";

        public string StatusSummary =>
            $"Available: {AvailableTools}  " +
            $"Borrowed: {BorrowedTools}  " +
            $"On Hold: {OnHoldTools}  " +
            $"Damaged: {DamagedTools}";

        // ── Constructor ────────────────────────────────────────
        public CatalogDisplayItem(
            EquipmentCatalog catalog)
        {
            Catalog = catalog;
        }
    }
}