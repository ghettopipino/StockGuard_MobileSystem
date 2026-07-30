using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class PreAssignment
    {
        [JsonProperty("toolId")]
        public string ToolId { get; set; } = string.Empty;
        [JsonProperty("toolName")]
        public string ToolName { get; set; } = string.Empty;
        [JsonProperty("workerId")]
        public string WorkerId { get; set; } = string.Empty;
        [JsonProperty("workerName")]
        public string WorkerName { get; set; } = string.Empty;
        [JsonProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;
        [JsonProperty("projectName")]
        public string ProjectName { get; set; } = string.Empty;
        [JsonProperty("assignedByName")]
        public string AssignedByName { get; set; } = string.Empty;
        [JsonProperty("status")]
        public string Status { get; set; } = "Pending";
        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}