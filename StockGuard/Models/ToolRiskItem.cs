using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    // New model classes (add to Models folder):
    public class ToolRiskItem
    {
        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public int IncidentCount { get; set; }
        public string Label => $"{ToolName} ({ToolId}) — {IncidentCount} incidents";
    }
}
