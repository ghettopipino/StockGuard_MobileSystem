using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
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


        [JsonProperty("borrowedProjectId")]
        public string BorrowedProjectId { get; set; } = string.Empty;

        [JsonProperty("borrowedProjectName")]
        public string BorrowedProjectName { get; set; } = string.Empty;
        [JsonProperty("holdProjectId")]
        public string HoldProjectId { get; set; } = string.Empty;

        [JsonProperty("holdProjectName")]
        public string HoldProjectName { get; set; } = string.Empty;

        [JsonProperty("holdLocation")]
        public string HoldLocation { get; set; } = string.Empty;

        [JsonProperty("lastBorrowerId")]
        public string LastBorrowerId { get; set; } = string.Empty;

        [JsonProperty("lastBorrowerName")]
        public string LastBorrowerName { get; set; } = string.Empty;

        [JsonProperty("holdDate")]
        public DateTime? HoldDate { get; set; }
        // Status helpers
        public bool IsDamaged => Status == "Damaged";
        public bool IsUnderRepair => Status == "UnderRepair";
        public bool IsLost => Status == "Lost";
        [JsonIgnore]
        public bool IsAvailable =>
    Status == "Available";
        
        [JsonIgnore]
        public bool IsBorrowed =>
            Status == "Borrowed";

        [JsonIgnore]
        public bool IsOnHold =>
            Status == "OnHold";

        [JsonIgnore]
        public bool IsPendingPause =>
            Status == "PendingPause";

        
        public string StatusColor => Status switch
        {
            "Available" => "#10b981",
            "Borrowed" => "#3b82f6",
            "PendingPause" => "#f59e0b",
            "OnHold" => "#8b5cf6",
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
            "PendingPause" => "⏸️",
            "OnHold" => "🔒",
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
        public string? PreAssignedWorkerId { get; set; }
        public string? PreAssignedWorkerName { get; set; }
    }
}