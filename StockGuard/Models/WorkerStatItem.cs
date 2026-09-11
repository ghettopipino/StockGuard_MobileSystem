namespace StockGuard.Models
{
    /// <summary>
    /// Represents one Worker in the Worker Activity
    /// section of ProjectAnalyticsView.
    /// </summary>
    public class WorkerStatItem
    {
        // ─────────────────────────────────────────────────────────
        // WORKER
        // ─────────────────────────────────────────────────────────

        public string WorkerId { get; set; } =
            string.Empty;

        public string WorkerName { get; set; } =
            string.Empty;


        // ─────────────────────────────────────────────────────────
        // ACTIVITY
        // ─────────────────────────────────────────────────────────

        // Number of equipment assignments accepted
        // by this Worker.
        public int AssignmentsAccepted { get; set; }


        // Number of completed equipment returns
        // involving this Worker.
        public int Returns { get; set; }


        // Number of damage reports where this Worker
        // was responsible for the equipment at that time.
        //
        // This does NOT mean the Worker caused the damage.
        public int Damages { get; set; }


        // ─────────────────────────────────────────────────────────
        // DISPLAY HELPERS
        // ─────────────────────────────────────────────────────────

        public string WorkerInitials =>
            string.IsNullOrWhiteSpace(
                WorkerName)
                ? "?"
                : WorkerName.Length >= 2
                    ? WorkerName[..2]
                        .ToUpper()
                    : WorkerName
                        .ToUpper();


        public bool HasDamages =>
            Damages > 0;


        // Used only for determining
        // the Most Active Worker.
        public int TotalActivity =>
            AssignmentsAccepted +
            Returns;
    }
}