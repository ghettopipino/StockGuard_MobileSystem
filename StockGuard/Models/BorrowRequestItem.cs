using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class BorrowRequestItem
    {
        public BorrowRequest Request { get; }
        public string RequestKey { get; set; }
            = string.Empty;

        // ✅ New transfer properties
        public bool IsTransfer { get; set; }
            = false;
        public TransferRequest? TransferRequest { get; set; }

        public string ToolId => Request.ToolId;
        public string ToolName => Request.ToolName;
        public string RequesterName => Request.RequesterName;
        public string OwnerId => Request.OwnerId;
        public string Status => Request.Status;

        // ✅ Different label for transfer vs borrow
        public string RequestTypeLabel =>
            IsTransfer ? "🔄 Transfer Request"
                       : "📩 Borrow Request";

        public string RequestTypeColor =>
            IsTransfer
                ? "#3b82f6"
                : "#f59e0b";

        public string StatusIcon => Status switch
        {
            "Pending" => "⏳",
            "Approved" => "✅",
            "Accepted" => "✅",
            "Declined" => "❌",
            _ => "❓"
        };

        public string StatusColor => Status switch
        {
            "Pending" => "#f59e0b",
            "Approved" => "#10b981",
            "Accepted" => "#10b981",
            "Declined" => "#ef4444",
            _ => "#94a3b8"
        };

        public string DateLabel
        {
            get
            {
                var ts = DateTime.Now - Request.RequestDate;
                if (ts.TotalMinutes < 1) return "Just now";
                if (ts.TotalMinutes < 60)
                    return $"{(int)ts.TotalMinutes}m ago";
                if (ts.TotalHours < 24)
                    return $"{(int)ts.TotalHours}h ago";
                if (ts.TotalDays < 2) return "Yesterday";
                return Request.RequestDate.ToString("MMM d");
            }
        }

        public bool IsPending => Status == "Pending";

        public BorrowRequestItem(BorrowRequest request)
        {
            Request = request;
        }
        public string RequestDescription
        {
            get
            {
                if (IsTransfer && TransferRequest != null)
                {
                    return $"{TransferRequest.FromWorkerName} wants to transfer " +
                           $"{TransferRequest.ToolName} to you.";
                }

                return $"{RequesterName} wants to borrow {ToolName}.";
            }
        }
    }
}
