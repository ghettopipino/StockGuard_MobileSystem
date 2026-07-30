namespace StockGuard.Models
{
    /// <summary>
    /// Represents one row in the Tool Usage section
    /// of ProjectAnalyticsView. Mirrors the anonymous type
    /// produced by the web AnalyticsController:
    ///   { Tool, Usage, Damages }
    /// </summary>
    public class ToolStatItem
    {
        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Usage { get; set; }
        public int Damages { get; set; }

        // ── Computed display properties ───────────────────────────────────────

        /// <summary>Drives the red damage badge visibility in the XAML.</summary>
        public bool HasDamages => Damages > 0;

        /// <summary>Matches the StatusColor computed property on the Tool model.</summary>
        public string StatusColor => Status switch
        {
            "Available" => "#10b981",
            "Borrowed" => "#3b82f6",
            "Damaged" => "#ef4444",
            "UnderRepair" => "#f59e0b",
            "Lost" => "#ef4444",
            _ => "#94a3b8"
        };
    }
}