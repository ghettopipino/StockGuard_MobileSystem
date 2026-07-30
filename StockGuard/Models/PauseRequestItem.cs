using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace StockGuard.Models
{
    public class PauseRequestItem
    {
        public PauseRequest Request { get; }
        public string RequestKey { get; }

        public string ToolId => Request.ToolId;
        public string ToolName => Request.ToolName;
        public string WorkerId => Request.WorkerId;
        public string WorkerName => Request.WorkerName;
        public string ProjectName => Request.ProjectName;
        public string Reason => Request.Reason;
        public string Status => Request.Status;
        public string StatusIcon => Request.StatusIcon;
        public string StatusColor => Request.StatusColor;
        public string DateLabel => Request.DateLabel;
        public bool IsPending => Request.IsPending;

        public PauseRequestItem(
            PauseRequest request,
            string requestKey)
        {
            Request = request;
            RequestKey = requestKey;
        }
    }
}
