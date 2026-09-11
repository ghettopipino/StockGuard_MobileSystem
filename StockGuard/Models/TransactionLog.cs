using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class TransactionLog
    {
        // ─────────────────────────────────────────────────────────
        // ID
        // ─────────────────────────────────────────────────────────

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; } =
            string.Empty;


        // ─────────────────────────────────────────────────────────
        // EQUIPMENT
        // ─────────────────────────────────────────────────────────

        [JsonProperty("toolId")]
        public string ToolId { get; set; } =
            string.Empty;

        [JsonProperty("toolName")]
        public string ToolName { get; set; } =
            string.Empty;


        // ─────────────────────────────────────────────────────────
        // PROJECT
        // ─────────────────────────────────────────────────────────

        [JsonProperty("projectId")]
        public string ProjectId { get; set; } =
            string.Empty;

        [JsonProperty("projectName")]
        public string ProjectName { get; set; } =
            string.Empty;


        // ─────────────────────────────────────────────────────────
        // RESPONSIBLE WORKER
        // ─────────────────────────────────────────────────────────

        [JsonProperty("workerId")]
        public string WorkerId { get; set; } =
            string.Empty;

        [JsonProperty("workerName")]
        public string WorkerName { get; set; } =
            string.Empty;


        // ─────────────────────────────────────────────────────────
        // PERSON WHO PERFORMED THE ACTION
        // ─────────────────────────────────────────────────────────

        [JsonProperty("performedById")]
        public string PerformedById { get; set; } =
            string.Empty;

        [JsonProperty("performedByName")]
        public string PerformedByName { get; set; } =
            string.Empty;


        // ─────────────────────────────────────────────────────────
        // ACTIVITY
        // ─────────────────────────────────────────────────────────

        [JsonProperty("action")]
        public string Action { get; set; } =
            string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } =
            string.Empty;

        [JsonProperty("condition")]
        public string Condition { get; set; } =
            "Good";

        [JsonProperty("date")]
        public DateTime Date { get; set; } =
            DateTime.Now;


        // ─────────────────────────────────────────────────────────
        // DISPLAY HELPERS
        // ─────────────────────────────────────────────────────────

        [JsonIgnore]
        public string WorkerDisplay =>
            string.IsNullOrWhiteSpace(
                WorkerName)
                ? "—"
                : WorkerName;


        [JsonIgnore]
        public string ProjectDisplay =>
            string.IsNullOrWhiteSpace(
                ProjectName)
                ? "—"
                : ProjectName;


        [JsonIgnore]
        public string PerformedByDisplay =>
            string.IsNullOrWhiteSpace(
                PerformedByName)
                ? "Not recorded"
                : PerformedByName;


        // ─────────────────────────────────────────────────────────
        // FONT AWESOME ACTION ICON
        // ─────────────────────────────────────────────────────────

        [JsonIgnore]
        public string ActionIcon => Action switch
        {
            "Borrowed" =>
                "\uf466",

            "Assignment Accepted" =>
                "\uf058",

            "End Day Check-In" =>
                "\uf3c5",

            "End Day Check-In Verified" =>
                "\uf058",

            "End Day Check-In Rejected" =>
                "\uf057",

            "Damage Found During Check-In" =>
                "\uf071",

            "Return Requested" =>
                "\uf2f6",

            "Returned" =>
                "\uf058",

            "Returned Damaged" =>
                "\uf071",

            "Return Rejected" =>
                "\uf057",

            "Damage Reported" =>
                "\uf071",

            "Damaged" =>
                "\uf071",

            "UnderRepair" =>
                "\uf0ad",

            "Resolved" =>
                "\uf058",

            "Repaired" =>
                "\uf0ad",

            "Missing Reported" =>
                "\uf128",

            "Lost Declared" =>
                "\uf128",

            "Equipment Found" =>
                "\uf058",

            "Missing Report Resolved" =>
                "\uf058",

            "Transferred" =>
                "\uf362",

            "Declined" =>
                "\uf057",

            _ =>
                "\uf1da"
        };


        // ─────────────────────────────────────────────────────────
        // ACTION COLOR
        // ─────────────────────────────────────────────────────────

        [JsonIgnore]
        public string ActionColor => Action switch
        {
            "Borrowed" =>
                "#3b82f6",

            "Assignment Accepted" =>
                "#10b981",

            "End Day Check-In" =>
                "#3b82f6",

            "End Day Check-In Verified" =>
                "#10b981",

            "End Day Check-In Rejected" =>
                "#ef4444",

            "Damage Found During Check-In" =>
                "#ef4444",

            "Return Requested" =>
                "#f59e0b",

            "Returned" =>
                "#10b981",

            "Returned Damaged" =>
                "#ef4444",

            "Return Rejected" =>
                "#ef4444",

            "Damage Reported" =>
                "#ef4444",

            "Damaged" =>
                "#ef4444",

            "UnderRepair" =>
                "#f59e0b",

            "Resolved" =>
                "#10b981",

            "Repaired" =>
                "#10b981",

            // Missing/Lost uses gray styling.
            "Missing Reported" =>
                "#6b7280",

            "Lost Declared" =>
                "#6b7280",

            "Equipment Found" =>
                "#10b981",

            "Missing Report Resolved" =>
                "#10b981",

            "Transferred" =>
                "#3b82f6",

            "Declined" =>
                "#ef4444",

            _ =>
                "#94a3b8"
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
                    return
                        $"{(int)ts.TotalMinutes}m ago";


                if (ts.TotalHours < 24)
                    return
                        $"{(int)ts.TotalHours}h ago";


                if (ts.TotalDays < 2)
                    return "Yesterday";


                if (ts.TotalDays < 7)
                    return
                        $"{(int)ts.TotalDays} days ago";


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