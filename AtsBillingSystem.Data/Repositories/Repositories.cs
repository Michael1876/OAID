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
            var totalCount = await _context.Subscribers.CountAsync();

            var items = await _context.Subscribers
                .AsNoTracking()
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

            return new PagedResult<DomainSubscriber>(items, totalCount, page, pageSize);
        }

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
                _context.Entry(entity).Property(e => e.RowVersion).OriginalValue = domainSubscriber.RowVersion;
            }
        }

        public Task UpdateBalancesBatchAsync(IEnumerable<DomainSubscriber> subscribersToUpdate)
        {
            foreach (var subDomain in subscribersToUpdate)
            {
                var entity = new SubscriberEntity { Id = subDomain.Id };
                _context.Subscribers.Attach(entity);
                entity.Balance = subDomain.Balance;

                _context.Entry(entity).Property(x => x.Balance).IsModified = true;
            }
            return Task.CompletedTask;
        }

        public async Task AddAsync(DomainSubscriber subscriber)
        {
            if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));

            var entity = new SubscriberEntity
            {
                Id = subscriber.Id,
                FullName = subscriber.FullName,
                ContractNumber = subscriber.ContractNumber,
                PhoneNumber = subscriber.PhoneNumber,
                Balance = subscriber.Balance,
                IsActive = subscriber.IsActive,
                TariffId = subscriber.TariffId,
                RowVersion = subscriber.RowVersion
            };

            await _context.Subscribers.AddAsync(entity);
        }
    }
}