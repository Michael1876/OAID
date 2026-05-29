using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.Infrastructure.Json.Api;

namespace AtsBillingSystem.Infrastructure.Json.Repositories;

public sealed class JsonUnitOfWork : IUnitOfWork
{
    private readonly ISimulatedJsonApiClient _apiClient;

    public JsonUnitOfWork(ISimulatedJsonApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task BeginTransactionAsync() => Task.CompletedTask;

    public Task CommitTransactionAsync() => Task.CompletedTask;

    public async Task RollbackTransactionAsync()
    {
        await _apiClient.ReloadAllAsync().ConfigureAwait(false);
    }

    public Task SaveChangesAsync() => _apiClient.PushAllAsync();
}
