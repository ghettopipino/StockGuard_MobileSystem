using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class Tool
    {
        // ─────────────────────────────────────────────────────────
        // IDENTITY
        // ─────────────────────────────────────────────────────────

        public string ToolId { get; set; } = string.Empty;

        public string ToolName { get; set; } = string.Empty;

        public string CatalogId { get; set; } = string.Empty;

        public string QrCode { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;


        // ─────────────────────────────────────────────────────────
        // CURRENT STATUS
        // ─────────────────────────────────────────────────────────

        // Available
        // Borrowed
        // PendingReturn
        // Damaged
        // UnderRepair
        // Lost
        public string Status { get; set; } = "Available";

        public string Condition { get; set; } = "Good";


        // ─────────────────────────────────────────────────────────
        // CURRENT WORKER
        // ─────────────────────────────────────────────────────────

        public string AssignedWorkerId { get; set; } = string.Empty;

        public string AssignedWorkerName { get; set; } = string.Empty;

        public DateTime? BorrowDate { get; set; }


        // ─────────────────────────────────────────────────────────
        // PROJECT
        // ─────────────────────────────────────────────────────────

        [JsonProperty("borrowedProjectId")]
        public string BorrowedProjectId { get; set; } = string.Empty;

        [JsonProperty("borrowedProjectName")]
        public string BorrowedProjectName { get; set; } = string.Empty;


        // ─────────────────────────────────────────────────────────
        // PROJECT ENGINEER WHO ASSIGNED THE TOOL
        // ─────────────────────────────────────────────────────────

        [JsonProperty("assignedById")]
        public string AssignedById { get; set; } = string.Empty;

        [JsonProperty("assignedByName")]
        public string AssignedByName { get; set; } = string.Empty;


        // ─────────────────────────────────────────────────────────
        // PENDING WORKER ASSIGNMENT
        // ─────────────────────────────────────────────────────────

        public string? PreAssignedWorkerId { get; set; }

        public string? PreAssignedWorkerName { get; set; }


        // ─────────────────────────────────────────────────────────
        // END-OF-DAY CHECK-IN
        // ─────────────────────────────────────────────────────────
        //
        // IMPORTANT:
        // Check-in does NOT change Status.
        // The tool remains Borrowed under the same worker/project.
        //

        [JsonProperty("lastCheckInLocation")]
        public string LastCheckInLocation { get; set; } = string.Empty;

        [JsonProperty("lastCheckInDate")]
        public DateTime? LastCheckInDate { get; set; }

        [JsonProperty("isCheckInPending")]
        public bool IsCheckInPending { get; set; } = false;

        [JsonProperty("lastCheckInVerifiedById")]
        public string LastCheckInVerifiedById { get; set; } = string.Empty;

        [JsonProperty("lastCheckInVerifiedByName")]
        public string LastCheckInVerifiedByName { get; set; } = string.Empty;


        // ─────────────────────────────────────────────────────────
        // STATUS HELPERS
        // ─────────────────────────────────────────────────────────

        [JsonIgnore]
        public bool IsAvailable =>
            Status == "Available";

        [JsonIgnore]
        public bool IsBorrowed =>
            Status == "Borrowed";

        [JsonIgnore]
        public bool IsPendingReturn =>
            Status == "PendingReturn";

        [JsonIgnore]
        public bool IsDamaged =>
            Status == "Damaged";

        [JsonIgnore]
        public bool IsUnderRepair =>
            Status == "UnderRepair";

        [JsonIgnore]
        public bool IsLost =>
            Status == "Lost";


        // Worker/project information should remain visible
        // for Borrowed, PendingReturn, or worker-reported Damaged tools.
        [JsonIgnore]
        public bool HasAssignmentInfo =>
            !string.IsNullOrWhiteSpace(AssignedWorkerId) ||
            !string.IsNullOrWhiteSpace(BorrowedProjectId);


        // ─────────────────────────────────────────────────────────
        // STATUS DISPLAY
        // ─────────────────────────────────────────────────────────

        [JsonIgnore]
        public string StatusColor => Status switch
        {
            "Available" => "#10b981",
            "Borrowed" => "#3b82f6",
            "PendingReturn" => "#f59e0b",
            "Damaged" => "#ef4444",
            "UnderRepair" => "#f97316",
            "Lost" => "#6b7280",
            _ => "#94a3b8"
        };

        [JsonIgnore]
        public string StatusIcon => Status switch
        {
            "Available" => "✅",
            "Borrowed" => "📦",
            "PendingReturn" => "⏳",
            "Damaged" => "⚠️",
            "UnderRepair" => "🔨",
            "Lost" => "❌",
            _ => "❓"
        };


        // ─────────────────────────────────────────────────────────
        // TOOL ICON
        // ─────────────────────────────────────────────────────────

        [JsonIgnore]
        public string ToolIcon => ToolName switch
        {
            var n when n.Contains(
                "Drill",
                StringComparison.OrdinalIgnoreCase)
                => "🔩",

            var n when n.Contains(
                "Hammer",
                StringComparison.OrdinalIgnoreCase)
                => "🔨",

            var n when n.Contains(
                "Ruler",
                StringComparison.OrdinalIgnoreCase)
                => "📏",

            var n when n.Contains(
                "Saw",
                StringComparison.OrdinalIgnoreCase)
                => "🪚",

            var n when n.Contains(
                "Wrench",
                StringComparison.OrdinalIgnoreCase)
                => "🔧",

            _ => "🔧"
        };
    }
}