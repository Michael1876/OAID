using AtsBillingSystem.Data.Entities;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Data.Mappers
{
    public static class SubscriberMapper
    {
        // Из БД -> В Ядро
        public static DomainSubscriber ToDomain(this SubscriberEntity entity)
        {
            return new DomainSubscriber
            {
                Id = entity.Id,
                FullName = entity.FullName,
                ContractNumber = entity.ContractNumber,
                PhoneNumber = entity.PhoneNumber,
                Balance = entity.Balance,
                IsActive = entity.IsActive,
                TariffId = entity.TariffId,
                RowVersion = entity.RowVersion // Передаем версию в домен
            };
        }

        // Из Ядра -> В БД (обновление существующей сущности)
        public static void UpdateEntity(this DomainSubscriber domain, SubscriberEntity entity)
        {
            entity.FullName = domain.FullName;
            entity.ContractNumber = domain.ContractNumber;
            entity.PhoneNumber = domain.PhoneNumber;
            entity.Balance = domain.Balance;
            entity.IsActive = domain.IsActive;
            // RowVersion обновляет сама БД, мы его только отдаем контексту для проверки
        }
    }
}
// Аналогично делаются мапперы для CallRecord и Tariff.