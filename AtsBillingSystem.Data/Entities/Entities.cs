using System;
using System.Collections.Generic;

namespace AtsBillingSystem.Data.Entities
{
    public class SubscriberEntity
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public Guid TariffId { get; set; }

        // EF Core использует это поле для отслеживания версий строки.
        // При попытке обновить старую запись вылетит DbUpdateConcurrencyException
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Навигационные свойства (Navigation Properties)
        public TariffEntity? Tariff { get; set; }
        public ICollection<CallRecordEntity> CallRecords { get; set; } = new List<CallRecordEntity>();
    }
}