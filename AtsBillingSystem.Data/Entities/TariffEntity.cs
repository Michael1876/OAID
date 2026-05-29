namespace AtsBillingSystem.Data.Entities
{
    public class TariffEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal InternalMinutePrice { get; set; }
        public decimal CityMinutePrice { get; set; }
        public decimal ConnectionFee { get; set; }
        public decimal SubscriptionFee { get; set; }
        public bool IsArchived { get; set; }
        public ICollection<SubscriberEntity> Subscribers { get; set; } = new List<SubscriberEntity>();
    }

    public class CallRecordEntity
    {
        public Guid Id { get; set; }
        public Guid SubscriberId { get; set; }
        public string DestinationNumber { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationSeconds { get; set; }
        // Храним enum в БД как int
        public Domain.Enums.CallType CallType { get; set; }
        public decimal Cost { get; set; }
        public SubscriberEntity? Subscriber { get; set; }
    }

    public class AdminUserEntity
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}