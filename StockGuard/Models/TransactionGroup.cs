using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace StockGuard.Models
{
    public class TransactionGroup
        : ObservableCollection<TransactionLog>
    {
        public string DateLabel { get; }

        public TransactionGroup(string dateLabel)
            : base()
        {
            DateLabel = dateLabel;
        }
    }
}
