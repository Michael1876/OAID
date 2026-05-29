using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtsBillingSystem.Domain.Common
{
    public class BillingResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> FailedItems { get; set; } = new List<string>();
    }
}
