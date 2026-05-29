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
    public class CallLogRepository : ICallLogRepository
    {
        private readonly AppDbContext _context;

        public CallLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<DomainCallRecord>> GetByFilterAsync(DateTime? start, DateTime? end, string phoneFilter, int page, int pageSize)
        {
            var query = _context.CallRecords.AsNoTracking().AsQueryable();

            if (start.HasValue) query = query.Where(c => c.StartTime >= start.Value);
            if (end.HasValue) query = query.Where(c => c.StartTime <= end.Value);
            if (!string.IsNullOrWhiteSpace(phoneFilter))
                query = query.Where(c => c.DestinationNumber.Contains(phoneFilter)
                    || (c.Subscriber != null && c.Subscriber.PhoneNumber.Contains(phoneFilter)));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(); // Тут нужен ToDomain маппинг логов

            // Допустим мы написали маппер CallLogMapper.ToDomain()
            var domainItems = items.Select(i => new DomainCallRecord
            {
                Id = i.Id,
                DestinationNumber = i.DestinationNumber,
                Cost = i.Cost,
                DurationSeconds = i.DurationSeconds,
                StartTime = i.StartTime,
                CallType = i.CallType
            });

            return new PagedResult<DomainCallRecord>(domainItems, totalCount, page, pageSize);
        }

        public async Task AddRangeAsync(IEnumerable<DomainCallRecord> records)
        {
            var entities = records.Select(r => new CallRecordEntity
            {
                Id = Guid.NewGuid(),
                SubscriberId = r.SubscriberId,
                DestinationNumber = r.DestinationNumber,
                StartTime = r.StartTime,
                DurationSeconds = r.DurationSeconds,
                CallType = r.CallType,
                Cost = r.Cost
            });

            await _context.CallRecords.AddRangeAsync(entities);
        }
    }
}