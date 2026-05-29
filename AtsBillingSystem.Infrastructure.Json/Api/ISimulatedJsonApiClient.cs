namespace AtsBillingSystem.Infrastructure.Json.Api;

/// <summary>Имитация HTTP API, отдающего JSON с «сервера».</summary>
public interface ISimulatedJsonApiClient
{
    Task FetchAllAsync(CancellationToken cancellationToken = default);
    Task ReloadAllAsync(CancellationToken cancellationToken = default);
    Task PushAllAsync(CancellationToken cancellationToken = default);
    string ResolveEndpoint(string resource);
}
