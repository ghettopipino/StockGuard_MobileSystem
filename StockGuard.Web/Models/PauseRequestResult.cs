namespace StockGuard.Web.Models
{
    public class PauseRequestResult
    {
        public string Key { get; set; }
            = string.Empty;
        public PauseRequest Report { get; set; }
            = new();
    }
}