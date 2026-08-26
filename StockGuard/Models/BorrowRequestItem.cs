using System;

namespace StockGuard.Models
{
    public class BorrowRequestItem
    {
        public BorrowRequest Request { get; }

        public string RequestKey { get; set; }
            = string.Empty;

        // ─────────────────────────────────────────────
        // TRANSFER
        // ─────────────────────────────────────────────

        public bool IsTransfer { get; set; }
            = false;

        public TransferRequest? TransferRequest { get; set; }

        // ─────────────────────────────────────────────
        // BASIC REQUEST DATA
        // ─────────────────────────────────────────────

        public string ToolId =>
            Request.ToolId;

        public string ToolName =>
            Request.ToolName;

        public string RequesterName =>
            Request.RequesterName;

        public string RequesterId =>
            Request.RequesterId;

        public string OwnerId =>
            Request.OwnerId;

        public string OwnerName =>
            Request.OwnerName;

        public string Status =>
            Request.Status;

        // ─────────────────────────────────────────────
        // REQUESTER DISPLAY
        // ─────────────────────────────────────────────

        public string RequesterDisplay
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(
                        RequesterName))
                {
                    return RequesterName;
                }

                if (!string.IsNullOrWhiteSpace(
                        RequesterId))
                {
                    return RequesterId;
                }

                return "Unknown Worker";
            }
        }

        // ─────────────────────────────────────────────
        // REQUEST TYPE
        // ─────────────────────────────────────────────

        public string RequestTypeLabel =>
            IsTransfer
                ? "Transfer Request"
                : "Borrow Request";

        public string RequestTypeColor =>
            IsTransfer
                ? "#3b82f6"
                : "#f59e0b";

        // ─────────────────────────────────────────────
        // DESCRIPTION
        // ─────────────────────────────────────────────

        public string RequestDescription
        {
            get
            {
                if (IsTransfer &&
                    TransferRequest != null)
                {
                    var sender =
                        !string.IsNullOrWhiteSpace(
                            TransferRequest.FromWorkerName)
                            ? TransferRequest.FromWorkerName
                            : TransferRequest.FromWorkerId;

                    if (string.IsNullOrWhiteSpace(sender))
                    {
                        sender =
                            "Another worker";
                    }

                    return
                        $"{sender} wants to transfer " +
                        $"{TransferRequest.ToolName} to you.";
                }

                return
                    $"{RequesterDisplay} wants to borrow " +
                    $"{ToolName}.";
            }
        }

        // ─────────────────────────────────────────────
        // STATUS
        // ─────────────────────────────────────────────

        public string StatusIcon =>
            Status switch
            {
                "Pending" => "⏳",
                "Approved" => "✅",
                "Accepted" => "✅",
                "Declined" => "❌",
                "Rejected" => "❌",
                _ => "❓"
            };

        public string StatusColor =>
            Status switch
            {
                "Pending" => "#f59e0b",
                "Approved" => "#10b981",
                "Accepted" => "#10b981",
                "Declined" => "#ef4444",
                "Rejected" => "#ef4444",
                _ => "#94a3b8"
            };

        public bool IsPending =>
            Status == "Pending";

        // ─────────────────────────────────────────────
        // DATE
        // ─────────────────────────────────────────────

        public string DateLabel
        {
            get
            {
                var ts =
                    DateTime.Now -
                    Request.RequestDate;

                if (ts.TotalMinutes < 1)
                    return "Just now";

                if (ts.TotalMinutes < 60)
                {
                    return
                        $"{(int)ts.TotalMinutes}m ago";
                }

                if (ts.TotalHours < 24)
                {
                    return
                        $"{(int)ts.TotalHours}h ago";
                }

                if (ts.TotalDays < 2)
                    return "Yesterday";

                if (ts.TotalDays < 7)
                {
                    return
                        $"{(int)ts.TotalDays} days ago";
                }

                return Request.RequestDate
                    .ToString("MMM d");
            }
        }

        // ─────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────

        public BorrowRequestItem(
            BorrowRequest request)
        {
            Request =
                request ??
                throw new ArgumentNullException(
                    nameof(request));
        }
    }
}