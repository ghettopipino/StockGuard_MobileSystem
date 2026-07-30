namespace StockGuard.Models
{
    /// <summary>
    /// Represents one row in the Worker Performance section
    /// of ProjectAnalyticsView. Mirrors the anonymous type
    /// produced by the web AnalyticsController:
    ///   { Worker, Borrows, Damages }
    /// </summary>
    public class WorkerStatItem
    {
        public string WorkerId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public int Borrows { get; set; }
        public int Damages { get; set; }

        // ── Computed display properties ───────────────────────────────────────

        /// <summary>First two letters of the worker's name, used as avatar text.</summary>
        public string WorkerInitials =>
            string.IsNullOrWhiteSpace(WorkerName)
                ? "?"
                : WorkerName.Length >= 2
                    ? WorkerName[..2].ToUpper()
                    : WorkerName.ToUpper();

        /// <summary>Drives the red damage badge visibility in the XAML.</summary>
        public bool HasDamages => Damages > 0;
    }
}