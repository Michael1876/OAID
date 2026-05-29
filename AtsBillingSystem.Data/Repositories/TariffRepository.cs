using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AtsBillingSystem.Data.Context;
using AtsBillingSystem.Data.Mappers;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Data.Repositories
{
    public class TariffRepository : ITariffRepository
    {
        private readonly AppDbContext _context;

        public TariffRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DomainTariff>> GetActiveAsync()
        {
            var entities = await _context.Tariffs
                .AsNoTracking()
                .Where(t => !t.IsArchived)
                .ToListAsync();

            // Упрощенный маппинг прямо здесь (в идеале вынести в TariffMapper)
            return entities.Select(e => new DomainTariff
            {
                Id = e.Id,
                Name = e.Name,
                InternalMinutePrice = e.InternalMinutePrice,
                CityMinutePrice = e.CityMinutePrice,
                ConnectionFee = e.ConnectionFee,
                SubscriptionFee = e.SubscriptionFee,
                IsArchived = e.IsArchived
            });
        }

        public async Task ArchiveAsync(Guid id)
        {
            var entity = await _context.Tariffs.FindAsync(id);
            if (entity != null)
            {
                entity.IsArchived = true;
                // Сохранение вызовет UnitOfWork
            }
        }
    }
}