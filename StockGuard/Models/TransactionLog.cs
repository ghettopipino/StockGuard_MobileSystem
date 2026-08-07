using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class TransactionLog
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonProperty("toolId")]
        public string ToolId { get; set; } = string.Empty;
       
        [JsonProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonProperty("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonProperty("toolName")]
        public string ToolName { get; set; } = string.Empty;

        [JsonProperty("workerId")]
        public string WorkerId { get; set; } = string.Empty;

        [JsonProperty("workerName")]
        public string WorkerName { get; set; } = string.Empty;

        [JsonProperty("action")]
        public string Action { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("condition")]
        public string Condition { get; set; } = "Good";

        [JsonProperty("date")]
        public DateTime Date { get; set; } = DateTime.Now;

        // ✅ Not stored in Firebase — computed locally
        [JsonIgnore]
        public string ActionIcon => Action switch
        {
            "Borrowed" => "📦",
            "Returned" => "✅",
            "Transferred" => "🔄",
            "Damaged" => "⚠️",
            "Repaired" => "🔨",
            _ => "📋"
        };

        // ✅ Not stored in Firebase — computed locally
        [JsonIgnore]
        public string DateLabel
        {
            get
            {
                var ts = DateTime.Now - Date;
                if (ts.TotalMinutes < 1) return "Just now";
                if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes}m ago";
                if (ts.TotalHours < 24) return $"{(int)ts.TotalHours}h ago";
                if (ts.TotalDays < 2) return "Yesterday";
                if (ts.TotalDays < 7) return $"{(int)ts.TotalDays} days ago";
                return Date.ToString("MMM d");
            }
        }
    }
}