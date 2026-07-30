using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class WorkerActivityItem
    {
        public string WorkerName { get; set; } = string.Empty;
        public string WorkerInitials { get; set; } = string.Empty;
        public int AssignedTools { get; set; } = 0;
        public string Status { get; set; } = "Idle";

        public string StatusColor => Status == "Active"
            ? "#10b981"
            : "#94a3b8";

        public string ToolsLabel => AssignedTools == 1
            ? "1 tool assigned"
            : $"{AssignedTools} tools assigned";
    }
}