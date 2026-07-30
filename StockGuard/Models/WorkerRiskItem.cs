using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class WorkerRiskItem
    {
        public string WorkerId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public int IncidentCount { get; set; }
        public string Label => $"{WorkerName} — involved in {IncidentCount} reports";
    }
}
