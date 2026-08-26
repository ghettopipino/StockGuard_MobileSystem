using System;
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


        // ─────────────────────────────────────────────────────
        // UI HELPERS
        // ─────────────────────────────────────────────────────

        [JsonIgnore]
        public string CatalogIcon => CatalogName switch
        {
            var n when n.Contains(
                "Drill",
                StringComparison.OrdinalIgnoreCase)
                => "\uf0ad",

            var n when n.Contains(
                "Hammer",
                StringComparison.OrdinalIgnoreCase)
                => "\uf6e3",

            var n when n.Contains(
                "Ruler",
                StringComparison.OrdinalIgnoreCase)
                => "\uf545",

            var n when n.Contains(
                "Saw",
                StringComparison.OrdinalIgnoreCase)
                => "\uf0ad",

            var n when n.Contains(
                "Wrench",
                StringComparison.OrdinalIgnoreCase)
                => "\uf0ad",

            var n when n.Contains(
                "Level",
                StringComparison.OrdinalIgnoreCase)
                => "\uf545",

            var n when n.Contains(
                "Tape",
                StringComparison.OrdinalIgnoreCase)
                => "\uf545",

            var n when n.Contains(
                "Nail",
                StringComparison.OrdinalIgnoreCase)
                => "\uf0ad",

            _ => "\uf0ad"
        };


        [JsonIgnore]
        public string QuantityLabel =>
            Quantity == 1
                ? "1 tool"
                : $"{Quantity} tools";
    }
}