using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class TransferRequest
    {
        [JsonProperty("transferId")]
        public string TransferId { get; set; }
            = string.Empty;

        [JsonProperty("toolId")]
        public string ToolId { get; set; }
            = string.Empty;

        [JsonProperty("toolName")]
        public string ToolName { get; set; }
            = string.Empty;

        [JsonProperty("fromWorkerId")]
        public string FromWorkerId { get; set; }
            = string.Empty;

        [JsonProperty("fromWorkerName")]
        public string FromWorkerName { get; set; }
            = string.Empty;

        [JsonProperty("toWorkerId")]
        public string ToWorkerId { get; set; }
            = string.Empty;

        [JsonProperty("toWorkerName")]
        public string ToWorkerName { get; set; }
            = string.Empty;

        // Pending | Accepted | Declined
        [JsonProperty("status")]
        public string Status { get; set; } = "Pending";

        [JsonProperty("requestDate")]
        public DateTime RequestDate { get; set; }
            = DateTime.Now;
    }
}
