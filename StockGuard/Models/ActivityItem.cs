using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class ActivityItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;

        // ── NEW: actual DateTime so we can group by day ───────────
        // Set this whenever you create an ActivityItem.
        // DayLabel and TimeLabel are derived from it automatically.
        public DateTime Date { get; set; } = DateTime.MinValue;

        // "Today", "Yesterday", or "Mon, Jan 6"
        public string DayLabel
        {
            get
            {
                if (Date == DateTime.MinValue)
                    return string.Empty;
                if (Date.Date == DateTime.Today)
                    return "Today";
                if (Date.Date == DateTime.Today.AddDays(-1))
                    return "Yesterday";
                return Date.ToString("ddd, MMM d");
            }
        }

        // "9:45 AM"
        public string TimeLabel =>
            Date == DateTime.MinValue
                ? string.Empty
                : Date.ToString("h:mm tt");
    }
}