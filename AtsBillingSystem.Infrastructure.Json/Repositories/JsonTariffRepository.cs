using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Infrastructure.Json.Api;
using AtsBillingSystem.Infrastructure.Json.Storage;

namespace AtsBillingSystem.Infrastructure.Json.Repositories;

public sealed class JsonTariffRepository : ITariffRepository
{
    private readonly ISimulatedJsonApiClient _apiClient;
    private readonly JsonDataStore _store;

    public JsonTariffRepository(ISimulatedJsonApiClient apiClient, JsonDataStore store)
    {
        _apiClient = apiClient;
        _store = store;
    }

    public async Task<IEnumerable<DomainTariff>> GetActiveAsync()
    {
        await EnsureDataLoadedAsync();
        return _store.GetTariffsSnapshot().Where(t => !t.IsArchived).ToList();
    }

    public async Task ArchiveAsync(Guid id)
    {
        await EnsureDataLoadedAsync();
        _store.ArchiveTariff(id);
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
