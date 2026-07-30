namespace StockGuard.Web.Models
{
    public class TransactionViewModel
    {
        // ── Displayed records (current page slice only) ───────────────────────
        public List<TransactionLog> Transactions { get; set; } = new();

        // ── Stats — always computed from the FULL unfiltered dataset ─────────
        // These never change when the user pages through results.
        public int TotalCount { get; set; }
        public int BorrowCount { get; set; }
        public int ReturnCount { get; set; }
        public int DamageCount { get; set; }
        public int TransferCount { get; set; }

        // ── Filter state (preserved across pages) ────────────────────────────
        public string? SelectedAction { get; set; }
        public string? SearchText { get; set; }

        // ── Pagination metadata ───────────────────────────────────────────────
        /// <summary>Total records after filter applied (before paging).</summary>
        public int TotalFiltered { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;

        public int TotalPages =>
            TotalFiltered == 0
                ? 1
                : (int)Math.Ceiling(
                    (double)TotalFiltered / PageSize);

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public int FirstItemIndex =>
            TotalFiltered == 0
                ? 0
                : (CurrentPage - 1) * PageSize + 1;

        public int LastItemIndex =>
            Math.Min(CurrentPage * PageSize, TotalFiltered);

        /// <summary>
        /// Page numbers to show in the pagination bar.
        /// Always shows at most 5 pages centred around the current page,
        /// so the bar never gets too wide on tables with hundreds of pages.
        /// </summary>
        public IEnumerable<int> PageRange
        {
            get
            {
                int start = Math.Max(1, CurrentPage - 2);
                int end = Math.Min(TotalPages, start + 4);
                // Shift start left if we're near the end
                start = Math.Max(1, end - 4);
                return Enumerable.Range(start, end - start + 1);
            }
        }
    }
}