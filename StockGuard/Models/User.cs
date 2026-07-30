using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableAttribute = SQLite.TableAttribute;
using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class User
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        // ✅ This is the Firebase node key
        // Unique for every user — never collides
        [JsonProperty("uniqueKey")]
        public string UniqueKey { get; set; } = string.Empty;

        [JsonProperty("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonProperty("address")]
        public string Address {  get; set; } = string.Empty;

        [JsonProperty("password")]
        public string Password { get; set; } = string.Empty;

        [JsonProperty("role")]
        public string Role { get; set; } = "Worker";

        [JsonProperty("accountStatus")]
        public string AccountStatus { get; set; } = "Pending";

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        [JsonIgnore]
        public bool IsProjectEngineer => Role == "Project Engineer";

        [JsonIgnore]
        public bool IsWorker => Role == "Worker";

        [JsonIgnore]
        public string StatusColor => AccountStatus switch
        {
            "Approved" => "#1A7A4A",
            "Pending" => "#E65100",
            "Rejected" => "#7B1F1F",
            _ => "#555555"
        };
        [JsonIgnore]
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName))
                    return "W";
                var parts = FullName.Trim().Split(' ',
                    StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2
                    ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                    : FullName[0].ToString().ToUpper();
            }
        }
    }
}


