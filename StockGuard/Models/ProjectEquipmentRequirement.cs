using System;

namespace StockGuard.Models
{
    public class ProjectEquipmentRequirement
    {
        public string ProjectId { get; set; } = string.Empty;
        public string CatalogId { get; set; } = string.Empty;
        public string CatalogName { get; set; } = string.Empty;
        public int QuantityNeeded { get; set; }
    }

    /// <summary>
    /// Computed project equipment summary.
    ///
    /// QuantityNeeded   = planned requirement
    /// BorrowedCount    = actual physical tools borrowed into project
    /// DistributedCount = borrowed tools currently held by workers
    /// WithPECount      = borrowed tools still under PE accountability
    /// RemainingCount   = physical tools still needed from office
    /// </summary>
    public class CatalogStockSummary
    {
        public string CatalogId { get; set; } = string.Empty;

        public string CatalogName { get; set; } = string.Empty;

        public int QuantityNeeded { get; set; }

        public int BorrowedCount { get; set; }

        public int DistributedCount { get; set; }


        // ─────────────────────────────────────────────────
        // COMPUTED COUNTS
        // ─────────────────────────────────────────────────

        public int RemainingCount =>
            Math.Max(
                0,
                QuantityNeeded - BorrowedCount);


        public int WithPECount =>
            Math.Max(
                0,
                BorrowedCount - DistributedCount);


        // Compatibility with older bindings/code.
        // This is NOT company-wide physical availability.
        public int AvailableCount =>
            RemainingCount;
    }
}