using System;
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

        // ─────────────────────────────────────────────
        // CURRENT HOLDER
        // ─────────────────────────────────────────────

        [JsonProperty("fromWorkerId")]
        public string FromWorkerId { get; set; }
            = string.Empty;

        [JsonProperty("fromWorkerName")]
        public string FromWorkerName { get; set; }
            = string.Empty;

        // ─────────────────────────────────────────────
        // TARGET WORKER
        // ─────────────────────────────────────────────

        [JsonProperty("toWorkerId")]
        public string ToWorkerId { get; set; }
            = string.Empty;

        [JsonProperty("toWorkerName")]
        public string ToWorkerName { get; set; }
            = string.Empty;

        // ─────────────────────────────────────────────
        // PROJECT
        // ─────────────────────────────────────────────

        [JsonProperty("projectId")]
        public string ProjectId { get; set; }
            = string.Empty;

        [JsonProperty("projectName")]
        public string ProjectName { get; set; }
            = string.Empty;

        // ─────────────────────────────────────────────
        // CONDITION
        // ─────────────────────────────────────────────

        [JsonProperty("condition")]
        public string Condition { get; set; }
            = "Good";

        // ─────────────────────────────────────────────
        // STATUS
        // ─────────────────────────────────────────────

        // Pending | Accepted | Declined
        [JsonProperty("status")]
        public string Status { get; set; }
            = "Pending";

        [JsonProperty("requestDate")]
        public DateTime RequestDate { get; set; }
            = DateTime.Now;

        // ─────────────────────────────────────────────
        // REVIEW / RESPONSE
        // ─────────────────────────────────────────────

        [JsonProperty("reviewedDate")]
        public DateTime? ReviewedDate { get; set; }

        [JsonProperty("reviewedById")]
        public string ReviewedById { get; set; }
            = string.Empty;

        [JsonProperty("reviewedByName")]
        public string ReviewedByName { get; set; }
            = string.Empty;
    }
}