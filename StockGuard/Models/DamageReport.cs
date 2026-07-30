using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class DamageReport
    {
        // ── existing fields (unchanged) ────────────────────────
        public string ReportId { get; set; } = string.Empty;
        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string WorkerId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Minor";
        public string Status { get; set; } = "Pending";
        public DateTime ReportDate { get; set; } = DateTime.Now;

        // ── NEW: accountability fields ─────────────────────────
        // Who held the tool immediately before the incident
        public string LastHandlerId { get; set; } = string.Empty;
        public string LastHandlerName { get; set; } = string.Empty;

        // Primary custodian at time of report
        public string PrimaryCustodianId { get; set; } = string.Empty;
        public string PrimaryCustodianName { get; set; } = string.Empty;

        // How many hands the tool passed through before incident
        public int TransferCountBeforeIncident { get; set; } = 0;

        // "High" | "Medium" | "Low" — based on data completeness
        public string ConfidenceLevel { get; set; } = "Low";

        // NEW: dispute support
        public bool IsDisputed { get; set; } = false;

        // Key = workerId, Value = their statement
        public Dictionary<string, string> DisputeNotes { get; set; } = new();

        // NEW: repeat incident flag (computed, not stored)
        [Newtonsoft.Json.JsonIgnore]
        public int PriorIncidentCount { get; set; } = 0;

        [Newtonsoft.Json.JsonIgnore]
        public bool IsHighRiskTool => PriorIncidentCount >= 2;
    }
}
