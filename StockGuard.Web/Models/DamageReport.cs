using Newtonsoft.Json;

namespace StockGuard.Web.Models
{
    public class DamageReport
    {
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

        [JsonProperty("description")]
        public string Description { get; set; }
            = string.Empty;

        [JsonProperty("severity")]
        public string Severity { get; set; } = "Minor";

        [JsonProperty("status")]
        public string Status { get; set; } = "Pending";

        [JsonProperty("reportDate")]
        public DateTime ReportDate { get; set; }
            = DateTime.Now;

        public string StatusBadgeClass => Status switch
        {
            "Pending" => "badge bg-warning",
            "Resolved" => "badge bg-success",
            "UnderRepair" => "badge bg-primary",
            "Lost" => "badge bg-danger",
            _ => "badge bg-secondary"
        };

        public string SeverityBadgeClass =>
            Severity == "Major Damage"
                ? "badge bg-danger"
                : "badge bg-warning";
    }
}