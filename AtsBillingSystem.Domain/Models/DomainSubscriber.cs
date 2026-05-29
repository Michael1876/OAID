using System;

namespace AtsBillingSystem.Domain.Models
{
    public class DomainSubscriber
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }

        // ВОТ ЭТО ПОЛЕ МЫ ДОБАВИЛИ:
        public Guid TariffId { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public void DebitBalance(decimal amount)
        {
            if (amount < 0) throw new ArgumentException("Сумма списания не может быть отрицательной.");
            Balance -= amount;
        }

        public void CreditBalance(decimal amount)
        {
            if (amount < 0) throw new ArgumentException("Сумма пополнения не может быть отрицательной.");
            Balance += amount;
        }
    }
}