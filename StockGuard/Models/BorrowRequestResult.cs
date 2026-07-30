using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Models
{
    public class BorrowRequestResult
    {
        public string Key { get; set; }
            = string.Empty;
        public BorrowRequest Request { get; set; }
            = new();
    }
}