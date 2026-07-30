using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class DamageReportItem
    {
        public DamageReport Report { get; }
        public string ReportKey { get; }

        public string ToolId => Report.ToolId;
        public string ToolName => Report.ToolName;
        public string WorkerName => Report.WorkerName;
        public string Description => Report.Description;
        public string Severity => Report.Severity;
        public string Status => Report.Status;

        public string StatusIcon => Status switch
        {
            "Pending" => "⏳",
            "Resolved" => "✅",
            "UnderRepair" => "🔨",
            "Lost" => "❌",
            _ => "❓"
        };

        public string StatusColor => Status switch
        {
            "Pending" => "#f59e0b",
            "Resolved" => "#10b981",
            "UnderRepair" => "#3b82f6",
            "Lost" => "#ef4444",
            _ => "#94a3b8"
        };

        public string SeverityColor => Severity switch
        {
            "Major Damage" => "#ef4444",
            "Minor Damage" => "#f59e0b",
            _ => "#94a3b8"
        };

        public string DateLabel =>
            Report.ReportDate.Date == DateTime.Today
                ? $"Today {Report.ReportDate:h:mm tt}"
                : Report.ReportDate.Date ==
                  DateTime.Today.AddDays(-1)
                    ? $"Yesterday {Report.ReportDate:h:mm tt}"
                    : Report.ReportDate.ToString(
                        "MMM d, yyyy h:mm tt");

        public bool IsPending =>
            Status == "Pending";

        public DamageReportItem(
            DamageReport report,
            string reportKey)
        {
            Report = report;
            ReportKey = reportKey;
        }
    }
}
