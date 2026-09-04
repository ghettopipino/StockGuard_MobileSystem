using System;
using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class LostReport
    {
        public string ReportId { get; set; } = string.Empty;

        // ── Equipment ───────────────────────────────────────
        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;

        // ── Responsible Worker ──────────────────────────────
        public string WorkerId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;

        // ── Project ─────────────────────────────────────────
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;

        // ── Worker Missing Report ───────────────────────────
        public string MissingDescription { get; set; } = string.Empty;

        public DateTime ReportDate { get; set; } = DateTime.Now;

        // Pending / Lost / Resolved
        public string Status { get; set; } = "Pending";

        // ── PE Verification ─────────────────────────────────
        public DateTime? VerifiedDate { get; set; }

        public string VerifiedById { get; set; } = string.Empty;
        public string VerifiedByName { get; set; } = string.Empty;

        // ── Official Lost Declaration ───────────────────────
        public DateTime? LostDate { get; set; }

        // ── Found / Resolution ──────────────────────────────
        public DateTime? FoundDate { get; set; }

        // Good / Damaged
        public string FoundCondition { get; set; } = string.Empty;

        public string ResolutionNotes { get; set; } = string.Empty;

        // ── UI HELPERS ─────────────────────────────────────

        [JsonIgnore]
        public bool IsPending =>
            Status == "Pending";

        [JsonIgnore]
        public bool IsLost =>
            Status == "Lost";

        [JsonIgnore]
        public bool IsResolved =>
            Status == "Resolved";

        [JsonIgnore]
        public bool CanBeHandled =>
            Status == "Pending" ||
            Status == "Lost";

        [JsonIgnore]
        public string StatusDisplay =>
            Status switch
            {
                "Pending" => "Pending Verification",
                "Lost" => "Lost",
                "Resolved" => "Resolved",
                _ => Status
            };
    }
}