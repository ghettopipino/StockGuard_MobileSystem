using Newtonsoft.Json;
namespace StockGuard.Web.Models
{
    public class User
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("uniqueKey")]
        public string UniqueKey { get; set; }
            = string.Empty;
        [JsonProperty("fullName")]
        public string FullName { get; set; }
            = string.Empty;
        [JsonProperty("email")]
        public string Email { get; set; }
            = string.Empty;
        [JsonProperty("password")]
        public string Password { get; set; }
            = string.Empty;
        [JsonProperty("role")]
        public string Role { get; set; } = "Worker";
        [JsonProperty("accountStatus")]
        public string AccountStatus { get; set; }
            = "Pending";
        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; }
            = DateTime.Now;
        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        // ── Assignment tracking ───────────────────────────────────
        [JsonProperty("assignedProjectId")]
        public string AssignedProjectId { get; set; }
            = string.Empty;

        public bool IsAvailable =>
            string.IsNullOrEmpty(AssignedProjectId);

        public bool IsProjectEngineer =>
            Role == "Project Engineer";
        public bool IsWorker => Role == "Worker";
        public string StatusBadgeClass =>
            AccountStatus switch
            {
                "Approved" => "badge bg-success",
                "Pending" => "badge bg-warning",
                "Rejected" => "badge bg-danger",
                _ => "badge bg-secondary"
            };
    }
}