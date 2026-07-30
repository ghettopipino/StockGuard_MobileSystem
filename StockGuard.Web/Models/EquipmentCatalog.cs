using Newtonsoft.Json;

namespace StockGuard.Web.Models
{
    public class EquipmentCatalog
    {
        [JsonProperty("catalogId")]
        public string CatalogId { get; set; }
            = string.Empty;

        [JsonProperty("catalogName")]
        public string CatalogName { get; set; }
            = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; }
            = string.Empty;

        [JsonProperty("prefix")]
        public string Prefix { get; set; }
            = string.Empty;

        [JsonProperty("quantity")]
        public int Quantity { get; set; } = 0;

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; }
            = DateTime.Now;

        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; } = false;
    }
}