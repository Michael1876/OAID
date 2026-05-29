using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtsBillingSystem.Domain.Common
{
    public class ParsedCallDto
    {
        public string CallerPhone { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationSeconds { get; set; }
    }
}
