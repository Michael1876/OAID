using AtsBillingSystem.Data.Context;
using AtsBillingSystem.Data.Entities;
using AtsBillingSystem.Data.Mappers;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtsBillingSystem.Data.Repositories
{
    public class SubscriberRepository : ISubscriberRepository
    {
        private readonly AppDbContext _context;

        public SubscriberRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DomainSubscriber> GetByIdAsync(Guid id)
        {
            var entity = await _context.Subscribers
                .AsNoTracking()
                .FirstAsync(s => s.Id == id);

            return entity.ToDomain();
        }

        public async Task<PagedResult<DomainSubscriber>> GetPagedAsync(int page, int pageSize)
        {
            // 1. Считаем общее количество записей в БД (нужно для вычисления страниц в UI)
            var totalCount = await _context.Subscribers.CountAsync();

            // 2. Делаем эффективную выборку из PostgreSQL с использованием LIMIT/OFFSET
            var items = await _context.Subscribers
                .AsNoTracking() // Отключаем трекинг для Read-Only запроса, это сильно ускоряет выборку
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(entity => new DomainSubscriber
                {
                    Id = entity.Id,
                    FullName = entity.FullName,
                    ContractNumber = entity.ContractNumber,
                    PhoneNumber = entity.PhoneNumber,
                    Balance = entity.Balance,
                    IsActive = entity.IsActive,
                    TariffId = entity.TariffId,
                    RowVersion = entity.RowVersion
                })
                .ToListAsync();

            // 3. Возвращаем упакованный результат
            return new PagedResult<DomainSubscriber>(items, totalCount, page, pageSize);
        }

        // ЗАЩИТА ОТ N+1
        public async Task<Dictionary<string, DomainSubscriber>> GetByPhonesBatchAsync(IEnumerable<string> phones)
        {
            var entities = await _context.Subscribers
                .AsNoTracking()
                .Where(s => phones.Contains(s.PhoneNumber))
                .ToListAsync();

            return entities.ToDictionary(e => e.PhoneNumber, e => e.ToDomain());
        }

        public async Task UpdateAsync(DomainSubscriber domainSubscriber)
        {
            var entity = await _context.Subscribers.FindAsync(domainSubscriber.Id);
            if (entity != null)
            {
                domainSubscriber.UpdateEntity(entity);
                // ВАЖНО: устанавливаем оригинальный RowVersion, чтобы EF мог проверить, 
                // не изменил ли кто-то запись в БД, пока мы ее редактировали в UI
                _context.Entry(entity).Property(e => e.RowVersion).OriginalValue = domainSubscriber.RowVersion;
            }
        }

        public Task UpdateBalancesBatchAsync(IEnumerable<DomainSubscriber> subscribersToUpdate)
        {
            // Здесь в идеале использовать Bulk Update (например, efcore.bulkextensions),
            // но для стандартного EF мы аттачим измененные сущности к контексту
            foreach (var subDomain in subscribersToUpdate)
            {
                var entity = new SubscriberEntity { Id = subDomain.Id };
                _context.Subscribers.Attach(entity);
                entity.Balance = subDomain.Balance;

                // Помечаем только поле Balance как измененное (микро-оптимизация SQL-запроса)
                _context.Entry(entity).Property(x => x.Balance).IsModified = true;
            }
            // SaveChangesAsync будет вызван из UseCase через UnitOfWork!
            return Task.CompletedTask;
        }
    }
}