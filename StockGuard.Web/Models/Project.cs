using Newtonsoft.Json;

namespace StockGuard.Web.Models
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
        public string Status { get; set; } = "Active";

        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }
            = string.Empty;

        [JsonProperty("createdByName")]
        public string CreatedByName { get; set; }
            = string.Empty;

        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// UniqueKeys of workers currently assigned to this project.
        /// Persisted to Firebase so cross-project occupancy checks work
        /// across controller actions.
        /// </summary>
        [JsonProperty("assignedWorkerKeys")]
        public List<string> AssignedWorkerKeys { get; set; }
            = new List<string>();

        /// <summary>
        /// ToolIds of tools currently deployed to this project.
        /// Persisted to Firebase alongside the Tool.ProjectId field
        /// so both sides stay in sync.
        /// </summary>
        [JsonProperty("deployedToolIds")]
        public List<string> DeployedToolIds { get; set; }
            = new List<string>();

        // ── Computed display helpers ──────────────────────────────

        public string StatusBadgeClass => Status switch
        {
            "Active" => "badge bg-success",
            "Paused" => "badge bg-warning",
            "Completed" => "badge bg-primary",
            _ => "badge bg-secondary"
        };

        public string StartDateLabel =>
            StartDate.ToString("MMM d, yyyy");

        public string EndDateLabel =>
            EndDate.HasValue
                ? EndDate.Value.ToString("MMM d, yyyy")
                : "Ongoing";

        public string DurationLabel
        {
            get
            {
                var end = EndDate ?? DateTime.Now;
                var days = (int)(end - StartDate).TotalDays;
                return days == 1 ? "1 day" : $"{days} days";
            }
        }
    }
}