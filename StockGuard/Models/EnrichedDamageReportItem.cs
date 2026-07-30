using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class EnrichedDamageReportItem : DamageReportItem
    {
        // ── Custody timeline (built from TransactionLog) ───
        public List<CustodyEntry> CustodyTimeline { get; set; } = new();
        public string LastHandlerName { get; set; } = "Unknown";
        public string LastHandlerId { get; set; } = string.Empty;
        public string PrimaryCustodian { get; set; } = "Unknown";

        // ── Insight flags ──────────────────────────────────
        public int PriorIncidentCount { get; set; } = 0;
        public int TransferCountRecent { get; set; } = 0;
        public bool IsHighRiskTool => PriorIncidentCount >= 2;
        public bool IsFrequentlyTransferred => TransferCountRecent >= 3;
        public bool IsDisputed { get; set; } = false;
        public int DisputeNoteCount { get; set; } = 0;

        // ── Confidence ─────────────────────────────────────
        public string ConfidenceLevel { get; set; } = "Low";
        public string ConfidenceColor => ConfidenceLevel switch
        {
            "High" => "#10b981",
            "Medium" => "#f59e0b",
            _ => "#ef4444"
        };
        public string ConfidenceIcon => ConfidenceLevel switch
        {
            "High" => "🟢",
            "Medium" => "🟡",
            _ => "🔴"
        };

        // ── System insight message ─────────────────────────
        public string InsightMessage
        {
            get
            {
                if (IsHighRiskTool)
                    return $"⚠️ This tool has been involved in {PriorIncidentCount} prior incidents";
                if (IsFrequentlyTransferred)
                    return $"🔄 This tool was transferred {TransferCountRecent} times before this incident";
                if (IsDisputed)
                    return $"⚖️ This report is disputed — {DisputeNoteCount} statement(s) recorded";
                return string.Empty;
            }
        }
        public bool HasInsight => !string.IsNullOrEmpty(InsightMessage);

        public EnrichedDamageReportItem(DamageReport report, string key)
            : base(report, key) { }
    }

    public class CustodyEntry
    {
        public string WorkerName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string DateLabel => Date.ToString("MMM d, h:mm tt");
        public string ActionIcon => Action switch
        {
            "Borrowed" => "📦",
            "Returned" => "↩️",
            "Transferred" => "🔄",
            _ => "📋"
        };
    }
}
