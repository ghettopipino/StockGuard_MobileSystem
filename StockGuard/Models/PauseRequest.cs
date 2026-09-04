using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class PauseRequest
    {
        [JsonProperty("requestId")]
        public string RequestId { get; set; }
            = string.Empty;

        [JsonProperty("toolId")]
        public string ToolId { get; set; }
            = string.Empty;

        [JsonProperty("toolName")]
        public string ToolName { get; set; }
            = string.Empty;

        [JsonProperty("workerId")]
        public string WorkerId { get; set; }
            = string.Empty;

        [JsonProperty("workerName")]
        public string WorkerName { get; set; }
            = string.Empty;

        [JsonProperty("projectId")]
        public string ProjectId { get; set; }
            = string.Empty;

        [JsonProperty("projectName")]
        public string ProjectName { get; set; }
            = string.Empty;

        [JsonProperty("reason")]
        public string Reason { get; set; }
            = string.Empty;

        // Pending | Approved | Rejected
        [JsonProperty("status")]
        public string Status { get; set; }
            = "Pending";

        [JsonProperty("requestDate")]
        public DateTime RequestDate { get; set; }
            = DateTime.Now;



        [JsonIgnore]
        public string StatusIcon => Status switch
        {
            "Pending" => "⏳",
            "Approved" => "✅",
            "Rejected" => "❌",
            _ => "❓"
        };

        [JsonIgnore]
        public string StatusColor => Status switch
        {
            "Pending" => "#f59e0b",
            "Approved" => "#10b981",
            "Rejected" => "#ef4444",
            _ => "#94a3b8"
        };

        [JsonIgnore]
        public bool IsPending => Status == "Pending";

        [JsonIgnore]
        public string DateLabel
        {
            get
            {
                var ts = DateTime.Now - RequestDate;
                if (ts.TotalMinutes < 1)
                    return "Just now";
                if (ts.TotalMinutes < 60)
                    return $"{(int)ts.TotalMinutes}m ago";
                if (ts.TotalHours < 24)
                    return $"{(int)ts.TotalHours}h ago";
                if (ts.TotalDays < 2)
                    return "Yesterday";
                return RequestDate
                    .ToString("MMM d, yyyy");
            }
        }
    }
}
