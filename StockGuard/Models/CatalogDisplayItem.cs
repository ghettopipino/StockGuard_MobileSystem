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


        // ─────────────────────────────────────────────────────
        // PHYSICAL INVENTORY
        // ─────────────────────────────────────────────────────

        public int TotalTools { get; set; }

        // Actual tools whose Status == Available
        public int AvailableTools { get; set; }

        // Actual tools currently in worker/project custody
        public int BorrowedTools { get; set; }

        public int DamagedTools { get; set; }

        public int LostTools { get; set; }


        // ─────────────────────────────────────────────────────
        // PROJECT ALLOCATION
        // ─────────────────────────────────────────────────────

        // Total QuantityNeeded across active projects.
        public int AllocatedTools { get; set; }

        // Portion of project requirements that has not yet
        // been fulfilled by actual physical project tools.
        public int AwaitingDistributionTools { get; set; }

        // Available physical tools that are not needed to
        // satisfy outstanding active-project requirements.
        public int UnallocatedTools { get; set; }


        // ─────────────────────────────────────────────────────
        // LABELS
        // ─────────────────────────────────────────────────────

        public string AvailableLabel =>
            $"{AvailableTools} available";

        public string BorrowedLabel =>
            $"{BorrowedTools} borrowed";

        public string DamagedLabel =>
            $"{DamagedTools} damaged";

        public string LostLabel =>
            $"{LostTools} lost";

        public string AllocatedLabel =>
            $"{AllocatedTools} allocated";

        public string AwaitingDistributionLabel =>
            $"{AwaitingDistributionTools} awaiting distribution";

        public string UnallocatedLabel =>
            $"{UnallocatedTools} unallocated";

        public string TotalLabel =>
            TotalTools == 1
                ? "1 tool"
                : $"{TotalTools} tools";


        // ─────────────────────────────────────────────────────
        // STATUS SUMMARY
        // ─────────────────────────────────────────────────────

        public string StatusSummary =>
            $"Available: {AvailableTools}  " +
            $"Borrowed: {BorrowedTools}  " +
            $"Damaged: {DamagedTools}  " +
            $"Lost: {LostTools}";


        // ─────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────

        public CatalogDisplayItem(
            EquipmentCatalog catalog)
        {
            Catalog = catalog;
        }
    }
}