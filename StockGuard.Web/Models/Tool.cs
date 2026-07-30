using Newtonsoft.Json;

namespace StockGuard.Web.Models
{
    public class Tool
    {
        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string CatalogId { get; set; } = string.Empty;
        public string Status { get; set; } = "Available";
        public string AssignedWorkerId { get; set; } = string.Empty;
        public string AssignedWorkerName { get; set; } = string.Empty;
        public string Condition { get; set; } = "Good";
        public string QrCode { get; set; } = string.Empty;
        public DateTime? BorrowDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        [JsonProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;
        // Status helpers
        public bool IsAvailable => Status == "Available";
        public bool IsBorrowed => Status == "Borrowed";
        public bool IsDamaged => Status == "Damaged";
        public bool IsUnderRepair => Status == "UnderRepair";
        public bool IsLost => Status == "Lost";

        public string StatusColor => Status switch
        {
            "Available" => "#10b981",
            "Borrowed" => "#3b82f6",
            "Damaged" => "#ef4444",
            "UnderRepair" => "#f59e0b",
            "Lost" => "#6b7280",
            _ => "#6b7280"
        };

        public string StatusIcon => Status switch
        {
            "Available" => "✅",
            "Borrowed" => "📦",
            "Damaged" => "⚠️",
            "UnderRepair" => "🔨",
            "Lost" => "❌",
            _ => "❓"
        };

        public string ToolIcon => ToolName switch
        {
            var n when n.Contains("Drill") => "🔩",
            var n when n.Contains("Hammer") => "🔨",
            var n when n.Contains("Ruler") => "📏",
            var n when n.Contains("Saw") => "🪚",
            var n when n.Contains("Wrench") => "🔧",
            _ => "🔧"
        };
        public string StatusBadgeClass => Status switch
        {
            "Available" => "badge bg-success",
            "Borrowed" => "badge bg-primary",
            "Damaged" => "badge bg-danger",
            "UnderRepair" => "badge bg-warning text-dark",
            "Lost" => "badge bg-secondary",
            _ => "badge bg-secondary"
        };
       
    }
}
