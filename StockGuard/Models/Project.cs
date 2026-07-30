using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class Project
    {
        [JsonProperty("projectId")]
        public string ProjectId { get; set; }
            = string.Empty;

        [JsonProperty("projectName")]
        public string ProjectName { get; set; }
            = string.Empty;

        [JsonProperty("location")]
        public string Location { get; set; }
            = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; }
            = string.Empty;

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }
            = DateTime.Now;

        [JsonProperty("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonProperty("status")]
        // Active | Paused | Completed
        public string Status { get; set; } = "Active";

        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }
            = string.Empty;

        [JsonProperty("createdByName")]
        public string CreatedByName { get; set; }
            = string.Empty;

        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        [JsonIgnore]
        public string StatusIcon => Status switch
        {
            "Active" => "🟢",
            "Paused" => "🟡",
            "Completed" => "✅",
            _ => "❓"
        };

        [JsonIgnore]
        public string StatusColor => Status switch
        {
            "Active" => "#10b981",
            "Paused" => "#f59e0b",
            "Completed" => "#3b82f6",
            _ => "#94a3b8"
        };

        [JsonIgnore]
        public string DurationLabel
        {
            get
            {
                var end = EndDate ?? DateTime.Now;
                var days = (int)(end - StartDate).TotalDays;
                return days == 1
                    ? "1 day"
                    : $"{days} days";
            }
        }

        [JsonIgnore]
        public string StartDateLabel =>
            StartDate.ToString("MMM d, yyyy");

        [JsonIgnore]
        public string EndDateLabel =>
            EndDate.HasValue
                ? EndDate.Value.ToString("MMM d, yyyy")
                : "Ongoing";

        [JsonIgnore]
        public bool IsActive => Status == "Active";

        [JsonIgnore]
        public bool IsCompleted => Status == "Completed";
    }
}
