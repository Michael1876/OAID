using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Infrastructure.Json.Api;
using AtsBillingSystem.Infrastructure.Json.Storage;

namespace AtsBillingSystem.Infrastructure.Json.Repositories;

public sealed class JsonCallLogRepository : ICallLogRepository
{
    private readonly ISimulatedJsonApiClient _apiClient;
    private readonly JsonDataStore _store;
    private readonly ILogger<JsonCallLogRepository> _logger;

    public JsonCallLogRepository(ISimulatedJsonApiClient apiClient, JsonDataStore store, ILogger<JsonCallLogRepository> logger)
    {
        _apiClient = apiClient;
        _store = store;
        _logger = logger;
    }

    public async Task<PagedResult<DomainCallRecord>> GetByFilterAsync(
        DateTime? start, DateTime? end, string phoneFilter, int page, int pageSize)
    {
        try
        {
            await EnsureDataLoadedAsync();

            var query = _store.GetCallRecordsSnapshot().AsEnumerable();

            if (start.HasValue) query = query.Where(c => c.StartTime >= start.Value);
            if (end.HasValue) query = query.Where(c => c.StartTime <= end.Value);

            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                var lowerFilter = phoneFilter.ToLowerInvariant(); // Безопасно для всех версий .NET
                var subscribersByPhone = _store.GetSubscribersSnapshot()
                    .Where(s => !string.IsNullOrEmpty(s.PhoneNumber) && s.PhoneNumber.ToLowerInvariant().Contains(lowerFilter))
                    .Select(s => s.Id)
                    .ToHashSet();

                query = query.Where(c =>
                    (!string.IsNullOrEmpty(c.DestinationNumber) && c.DestinationNumber.ToLowerInvariant().Contains(lowerFilter))
                    || subscribersByPhone.Contains(c.SubscriberId));
            }

            var filtered = query.OrderByDescending(c => c.StartTime).ToList();
            var totalCount = filtered.Count;
            var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult<DomainCallRecord>(items, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске и фильтрации журнала звонков.");
            throw; // Прокидываем в ViewModel
        }
    }

    public async Task AddRangeAsync(IEnumerable<DomainCallRecord> records)
    {
        await EnsureDataLoadedAsync();
        _store.AddCallRecords(records);
    }

private async Task EnsureDataLoadedAsync()
{
    // Проверяем именно журнал звонков. Если он пуст — просим клиент загрузить данные.
    // SimulatedJsonApiClient под капотом сам разберется с блокировками через _fetchLock и флаг _isLoaded
    if (_store.GetCallRecordsSnapshot().Count == 0)
        await _apiClient.FetchAllAsync();
}
}