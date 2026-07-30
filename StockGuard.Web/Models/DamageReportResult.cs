namespace StockGuard.Web.Models
{
    public class DamageReportResult
    {
        public string Key { get; set; }
            = string.Empty;
        public DamageReport Report { get; set; }
            = new();
    }
}