using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class TransactionLog
    {
        // ─────────────────────────────────────────────────────────
        // ID
        // ─────────────────────────────────────────────────────────

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; } = string.Empty;


        // ─────────────────────────────────────────────────────────
        // EQUIPMENT
        // ─────────────────────────────────────────────────────────

        [JsonProperty("toolId")]
        public string ToolId { get; set; } = string.Empty;

        [JsonProperty("toolName")]
        public string ToolName { get; set; } = string.Empty;


        // ─────────────────────────────────────────────────────────
        // PROJECT
        // ─────────────────────────────────────────────────────────

        [JsonProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonProperty("projectName")]
        public string ProjectName { get; set; } = string.Empty;


        // ─────────────────────────────────────────────────────────
        // RESPONSIBLE WORKER
        // ─────────────────────────────────────────────────────────

        [JsonProperty("workerId")]
        public string WorkerId { get; set; } = string.Empty;

        [JsonProperty("workerName")]
        public string WorkerName { get; set; } = string.Empty;


        // ─────────────────────────────────────────────────────────
        // PERSON WHO PERFORMED THE ACTION
        // ─────────────────────────────────────────────────────────
        //
        // Worker actions:
        //   PerformedBy = worker
        //
        // PE actions:
        //   PerformedBy = Project Engineer
        //

        [JsonProperty("performedById")]
        public string PerformedById { get; set; } = string.Empty;

        [JsonProperty("performedByName")]
        public string PerformedByName { get; set; } = string.Empty;


        // ─────────────────────────────────────────────────────────
        // ACTIVITY
        // ─────────────────────────────────────────────────────────

        [JsonProperty("action")]
        public string Action { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("condition")]
        public string Condition { get; set; } = "Good";

        [JsonProperty("date")]
        public DateTime Date { get; set; } = DateTime.Now;


        // ─────────────────────────────────────────────────────────
        // DISPLAY HELPERS
        // ─────────────────────────────────────────────────────────

        [JsonIgnore]
        public string WorkerDisplay =>
            string.IsNullOrWhiteSpace(WorkerName)
                ? "—"
                : WorkerName;

        [JsonIgnore]
        public string ProjectDisplay =>
            string.IsNullOrWhiteSpace(ProjectName)
                ? "—"
                : ProjectName;

        [JsonIgnore]
        public string PerformedByDisplay =>
            string.IsNullOrWhiteSpace(PerformedByName)
                ? "Not recorded"
                : PerformedByName;


        // ─────────────────────────────────────────────────────────
        // ACTION ICON
        // ─────────────────────────────────────────────────────────

        [JsonIgnore]
        public string ActionIcon => Action switch
        {
            "Borrowed" =>
                "📦",

            "End Day Check-In" =>
                "📍",

            "End Day Check-In Verified" =>
                "✓",

            "Return Requested" =>
                "↩",

            "Returned" =>
                "✓",

            "Returned Damaged" =>
                "⚠",

            "Return Rejected" =>
                "↩",

            "Damage Reported" =>
                "⚠",

            "Damaged" =>
                "⚠",

            "UnderRepair" =>
                "🔨",

            "Resolved" =>
                "✓",

            "Repaired" =>
                "✓",

            "Lost" =>
                "!",

            "Transferred" =>
                "↔",

            "Declined" =>
                "×",

            _ =>
                "•"
        };


        // ─────────────────────────────────────────────────────────
        // DATE
        // ─────────────────────────────────────────────────────────

        [JsonIgnore]
        public string DateLabel
        {
            get
            {
                var ts =
                    DateTime.Now - Date;

                if (ts.TotalMinutes < 1)
                    return "Just now";

                if (ts.TotalMinutes < 60)
                    return $"{(int)ts.TotalMinutes}m ago";

                if (ts.TotalHours < 24)
                    return $"{(int)ts.TotalHours}h ago";

                if (ts.TotalDays < 2)
                    return "Yesterday";

                if (ts.TotalDays < 7)
                    return $"{(int)ts.TotalDays} days ago";

                return Date.ToString(
                    "MMM d, yyyy");
            }
        }

        [JsonIgnore]
        public string FullDateLabel =>
            Date.ToString(
                "MMM d, yyyy h:mm tt");
    }
}