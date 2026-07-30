using Newtonsoft.Json;

namespace StockGuard.Web.Models
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

        [JsonProperty("status")]
        public string Status { get; set; }
            = "Pending";

        [JsonProperty("requestDate")]
        public DateTime RequestDate { get; set; }
            = DateTime.Now;

        [JsonProperty("approvedDate")]
        public DateTime? ApprovedDate { get; set; }

        [JsonProperty("approvedBy")]
        public string ApprovedBy { get; set; }
            = string.Empty;

        public string StatusBadgeClass => Status switch
        {
            "Pending" => "badge bg-warning",
            "Approved" => "badge bg-success",
            "Rejected" => "badge bg-danger",
            _ => "badge bg-secondary"
        };

        public string DateLabel =>
            RequestDate.ToString(
                "MMM d, yyyy h:mm tt");
    }
}