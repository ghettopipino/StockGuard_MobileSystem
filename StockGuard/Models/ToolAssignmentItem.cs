using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class ToolAssignmentItem
    {
        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string Status { get; set; } = "Borrowed";
        public DateTime BorrowDate { get; set; }

        public string BorrowDuration
        {
            get
            {
                var ts = DateTime.Now - BorrowDate;
                if (ts.TotalMinutes < 60) return "Just now";
                if (ts.TotalHours < 24) return $"{(int)ts.TotalHours}h borrowed";
                if (ts.TotalDays < 2) return "1 day borrowed";
                return $"{(int)ts.TotalDays} days borrowed";
            }
        }

        public string BorrowDateLabel =>
            BorrowDate.Date == DateTime.Today
                ? $"Today {BorrowDate:h:mm tt}"
                : BorrowDate.Date == DateTime.Today.AddDays(-1)
                    ? $"Yesterday {BorrowDate:h:mm tt}"
                    : BorrowDate.ToString("MMM d");

        public string ToolIcon => ToolName switch
        {
            var n when n.Contains("Drill") => "🔩",
            var n when n.Contains("Hammer") => "🔨",
            var n when n.Contains("Ruler") => "📏",
            var n when n.Contains("Saw") => "🪚",
            var n when n.Contains("Wrench") => "🔧",
            _ => "🔧"
        };
    }
}
