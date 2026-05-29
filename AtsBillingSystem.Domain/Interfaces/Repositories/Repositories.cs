using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Domain.Common;

namespace AtsBillingSystem.Domain.Interfaces.Repositories
{
    public interface ISubscriberRepository
    {
        Task<DomainSubscriber> GetByIdAsync(Guid id);
        Task<PagedResult<DomainSubscriber>> GetPagedAsync(int page, int pageSize);
        // Защита от N+1: берем пачку номеров, отдаем словарь Номер -> Абонент
        Task<Dictionary<string, DomainSubscriber>> GetByPhonesBatchAsync(IEnumerable<string> phones);
        Task UpdateAsync(DomainSubscriber domainSubscriber);
        // Массовое обновление балансов за 1 запрос
        Task UpdateBalancesBatchAsync(IEnumerable<DomainSubscriber> newBalances);
    }

    public interface ICallLogRepository
    {
        Task<PagedResult<DomainCallRecord>> GetByFilterAsync(DateTime? start, DateTime? end, string phoneFilter, int page, int pageSize);
        Task AddRangeAsync(IEnumerable<DomainCallRecord> records);
    }

    public interface ITariffRepository
    {
        Task<IEnumerable<DomainTariff>> GetActiveAsync();
        Task ArchiveAsync(Guid id);
    }

    public interface IAuthRepository
    {
        Task<DomainAdminUser> GetByLoginAsync(string login);
    }
}