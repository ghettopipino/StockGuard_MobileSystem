using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class ReturnRequest
    {
        public string RequestId { get; set; } = string.Empty;

        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;

        public string WorkerId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;

        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;

        // What the worker reports when submitting the return
        public string ReportedCondition { get; set; } = "Good";

        // What the Project Engineer confirms after physical inspection
        public string VerifiedCondition { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        // Pending / Approved / Rejected
        public string Status { get; set; } = "Pending";

        public DateTime RequestDate { get; set; }

        public DateTime? ReviewedDate { get; set; }

        public string ReviewedById { get; set; } = string.Empty;
        public string ReviewedByName { get; set; } = string.Empty;
    }

    public class ReturnRequestResult
    {
        public string Key { get; set; } = string.Empty;
        public ReturnRequest Request { get; set; } = new();
    }
}
