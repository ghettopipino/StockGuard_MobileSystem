namespace StockGuard.Models
{
    /// <summary>
    /// Represents one worker in the Worker Performance
    /// section of ProjectAnalyticsView.
    /// </summary>
    public class WorkerStatItem
    {
        // ── WORKER ──────────────────────────────────────────

        public string WorkerId { get; set; } = string.Empty;

        public string WorkerName { get; set; } = string.Empty;


        // ── PERFORMANCE ─────────────────────────────────────

        // Number of equipment originally borrowed/accepted
        public int Borrows { get; set; }

        // Number of equipment received through transfer
        public int TransfersReceived { get; set; }

        // Number of damage reports involving this worker
        public int Damages { get; set; }


        // ── DISPLAY HELPERS ─────────────────────────────────

        public string WorkerInitials =>
            string.IsNullOrWhiteSpace(WorkerName)
                ? "?"
                : WorkerName.Length >= 2
                    ? WorkerName[..2].ToUpper()
                    : WorkerName.ToUpper();


        public bool HasDamages =>
            Damages > 0;

        public bool HasTransfers =>
            TransfersReceived > 0;


        // Total equipment-handling activity.
        // Useful for determining the Most Active Worker.
        public int TotalActivity =>
            Borrows + TransfersReceived;
    }
}