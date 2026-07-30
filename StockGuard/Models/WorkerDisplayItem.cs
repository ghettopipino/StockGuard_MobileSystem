using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class WorkerDisplayItem
    {
        public User Worker { get; }
        public int AssignedToolsCount { get; set; }

        public string FullName => Worker.FullName;
        public string Email => Worker.Email;
        
        public string PhoneNumber => Worker.PhoneNumber;

        public string Address => Worker.Address;

        public string AccountStatus => Worker.AccountStatus;
        public string UniqueKey => Worker.UniqueKey;

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Worker.FullName))
                    return "W";
                var parts = Worker.FullName.Trim()
                    .Split(' ',
                        StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2
                    ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                    : Worker.FullName[0].ToString().ToUpper();
            }
        }

        public string StatusColor => Worker.AccountStatus switch
        {
            "Approved" => "#10b981",
            "Pending" => "#f59e0b",
            "Rejected" => "#ef4444",
            _ => "#94a3b8"
        };

        public string StatusIcon => Worker.AccountStatus switch
        {
            "Approved" => "✅",
            "Pending" => "⏳",
            "Rejected" => "❌",
            _ => "❓"
        };

        public string ActivityStatus =>
            AssignedToolsCount > 0 ? "Active" : "Idle";

        public string ActivityColor =>
            AssignedToolsCount > 0 ? "#10b981" : "#94a3b8";

        public string ToolsLabel =>
            AssignedToolsCount == 1
                ? "1 tool assigned"
                : $"{AssignedToolsCount} tools assigned";

        public string JoinedLabel =>
            Worker.DateCreated.ToString("MMM d, yyyy");

        public WorkerDisplayItem(User worker)
        {
            Worker = worker;
        }
    }
}
