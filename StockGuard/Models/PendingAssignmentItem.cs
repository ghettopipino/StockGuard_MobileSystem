using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class PendingAssignmentItem
    {
        public string Key { get; set; } = string.Empty;
        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string AssignedByName { get; set; } = string.Empty;
        public PreAssignment? Assignment { get; set; }
    }
}