using Newtonsoft.Json;
using System.Threading;
using System.Xml.Linq;

namespace StockGuard.Web.Models
{
    public class TransactionLog
    {
        // ✅ Match exact JsonProperty names
        // used by mobile app
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }
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

        [JsonProperty("action")]
        public string Action { get; set; }
            = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; }
            = string.Empty;

        [JsonProperty("condition")]
        public string Condition { get; set; }
            = "Good";

        [JsonProperty("date")]
        public DateTime Date { get; set; }
            = DateTime.Now;

        // ── Computed ──────────────────────────────────────────────
        public string ActionBadgeClass => Action switch
        {
            "Borrowed" => "badge bg-primary",
            "Returned" => "badge bg-success",
            "Transferred" => "badge bg-info",
            "Damaged" => "badge bg-danger",
            "Repaired" => "badge bg-warning",
            _ => "badge bg-secondary"
        };

        public string DateLabel =>
            Date.ToString("MMM d, yyyy h:mm tt");
    }
}