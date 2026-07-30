using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class BorrowRequest
    {
        [JsonProperty("requestId")]
        public string RequestId { get; set; } = string.Empty;

        [JsonProperty("toolId")]
        public string ToolId { get; set; } = string.Empty;

        [JsonProperty("toolName")]
        public string ToolName { get; set; } = string.Empty;

        [JsonProperty("requesterId")]
        public string RequesterId { get; set; } = string.Empty;

        [JsonProperty("requesterName")]
        public string RequesterName { get; set; } = string.Empty;

        [JsonProperty("ownerId")]
        public string OwnerId { get; set; } = string.Empty;

        [JsonProperty("ownerName")]
        public string OwnerName { get; set; } = string.Empty;

        // Pending | Approved | Declined
        [JsonProperty("status")]
        public string Status { get; set; } = "Pending";

        [JsonProperty("requestDate")]
        public DateTime RequestDate { get; set; } = DateTime.Now;
    }
}
