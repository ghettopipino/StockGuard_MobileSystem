using System;

namespace StockGuard.Models
{
    public class DamageReport
    {
        public string ReportId { get; set; } = string.Empty;

        // ── Equipment ───────────────────────────────────────
        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;

        // ── Worker responsible at time of damage ────────────
        public string WorkerId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;

        // ── Project ─────────────────────────────────────────
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;

        // ── Project Engineer ────────────────────────────────
        public string ProjectEngineerId { get; set; } = string.Empty;
        public string ProjectEngineerName { get; set; } = string.Empty;

        // ── Damage Information ──────────────────────────────
        public string Description { get; set; } = string.Empty;

        // Minor / Major
        public string Severity { get; set; } = "Minor";

        // Pending / Reviewed / Resolved
        public string Status { get; set; } = "Pending";

        public DateTime ReportDate { get; set; } = DateTime.Now;

        // ── Review ──────────────────────────────────────────
        public DateTime? ReviewedDate { get; set; }

        public string ReviewedById { get; set; } = string.Empty;
        public string ReviewedByName { get; set; } = string.Empty;

        public string ResolutionNotes { get; set; } = string.Empty;
    }
}