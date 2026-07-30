using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StockGuard.Models
{
    public class EquipmentCatalog
    {
        [JsonProperty("catalogId")]
        public string CatalogId { get; set; } = string.Empty;

        [JsonProperty("catalogName")]
        public string CatalogName { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("prefix")]
        public string Prefix { get; set; } = string.Empty;

        [JsonProperty("quantity")]
        public int Quantity { get; set; } = 0;

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        [JsonIgnore]
        public string CatalogIcon => CatalogName switch
        {
            var n when n.Contains("Drill") => "🔩",
            var n when n.Contains("Hammer") => "🔨",
            var n when n.Contains("Ruler") => "📏",
            var n when n.Contains("Saw") => "🪚",
            var n when n.Contains("Wrench") => "🔧",
            var n when n.Contains("Level") => "📐",
            var n when n.Contains("Tape") => "📏",
            var n when n.Contains("Nail") => "🔩",
            _ => "🔧"
        };

        [JsonIgnore]
        public string QuantityLabel =>
            Quantity == 1
                ? "1 tool"
                : $"{Quantity} tools";
    }
}
