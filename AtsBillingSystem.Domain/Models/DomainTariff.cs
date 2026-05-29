using System;
using AtsBillingSystem.Domain.Enums;

namespace AtsBillingSystem.Domain.Models
{
    public class DomainTariff
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal InternalMinutePrice { get; set; }
        public decimal CityMinutePrice { get; set; }
        public decimal ConnectionFee { get; set; }
        public decimal SubscriptionFee { get; set; }
        public bool IsArchived { get; set; }
    }
}
