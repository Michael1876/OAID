using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Infrastructure.Json.Api;
using AtsBillingSystem.Infrastructure.Json.Storage;

namespace AtsBillingSystem.Infrastructure.Json.Repositories;

public sealed class JsonSubscriberRepository : ISubscriberRepository
{
    private readonly ISimulatedJsonApiClient _apiClient;
    private readonly JsonDataStore _store;

    public JsonSubscriberRepository(ISimulatedJsonApiClient apiClient, JsonDataStore store)
    {
        _apiClient = apiClient;
        _store = store;
    }

    public async Task<DomainSubscriber> GetByIdAsync(Guid id)
    {
        await EnsureDataLoadedAsync();
        return _store.FindSubscriber(id)
            ?? throw new KeyNotFoundException($"Абонент {id} не найден.");
    }

    public async Task<PagedResult<DomainSubscriber>> GetPagedAsync(int page, int pageSize)
    {
        await EnsureDataLoadedAsync();

        var all = _store.GetSubscribersSnapshot();
        var totalCount = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<DomainSubscriber>(items, totalCount, page, pageSize);
    }

    public async Task<Dictionary<string, DomainSubscriber>> GetByPhonesBatchAsync(IEnumerable<string> phones)
    {
        await EnsureDataLoadedAsync();

        var phoneSet = phones.ToHashSet();
        return _store.GetSubscribersSnapshot()
            .Where(s => phoneSet.Contains(s.PhoneNumber))
            .ToDictionary(s => s.PhoneNumber);
    }

    public async Task UpdateAsync(DomainSubscriber domainSubscriber)
    {
        await EnsureDataLoadedAsync();
        _store.UpdateSubscriber(domainSubscriber);
    }

    public async Task UpdateBalancesBatchAsync(IEnumerable<DomainSubscriber> subscribersToUpdate)
    {
        await EnsureDataLoadedAsync();
        _store.UpdateBalances(subscribersToUpdate);
    }

    public async Task AddAsync(DomainSubscriber subscriber)
    {
        if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));
        await EnsureDataLoadedAsync();

        // Потокобезопасно добавляем в кэш хранилища
        lock (_store)
        {
            _store.Subscribers.Add(subscriber);
        }
    }

    private async Task EnsureDataLoadedAsync()
    {
        if (_store.GetSubscribersSnapshot().Count == 0
            && _store.GetTariffsSnapshot().Count == 0
            && _store.GetCallRecordsSnapshot().Count == 0)
        {
            await _apiClient.FetchAllAsync();
        }
    }
}