using System;
using AtsBillingSystem.Domain.Enums;

namespace AtsBillingSystem.Domain.Models
{
    public class DomainCallRecord
    {
        public Guid Id { get; set; }
        public Guid SubscriberId { get; set; }
        public string DestinationNumber { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationSeconds { get; set; }
        public CallType CallType { get; set; }
        public decimal Cost { get; set; }
    }
}
