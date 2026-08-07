using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class ProjectEquipmentRequirement
    {
        public string ProjectId { get; set; } = string.Empty;
        public string CatalogId { get; set; } = string.Empty;
        public string CatalogName { get; set; } = string.Empty;
        public int QuantityNeeded { get; set; }
    }

    /// <summary>Computed, not stored — Available/Borrowed for one catalog on one project.</summary>
    public class CatalogStockSummary
    {
        public string CatalogId { get; set; } = string.Empty;
        public string CatalogName { get; set; } = string.Empty;
        public int QuantityNeeded { get; set; }
        public int BorrowedCount { get; set; }
        public int AvailableCount => Math.Max(0, QuantityNeeded - BorrowedCount);
    }
}