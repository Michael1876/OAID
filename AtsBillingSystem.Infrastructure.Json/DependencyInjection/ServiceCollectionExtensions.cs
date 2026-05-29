using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Infrastructure.Json.Api;
using AtsBillingSystem.Infrastructure.Json.Repositories;
using AtsBillingSystem.Infrastructure.Json.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AtsBillingSystem.Infrastructure.Json.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJsonApiDataLayer(this IServiceCollection services, Action<JsonApiOptions>? configure = null)
    {
        var options = new JsonApiOptions();
        configure?.Invoke(options);

        services.AddLogging(); // Обязательно подключаем логирование (если не подключено на уровне WPF App)

        services.AddSingleton(options);
        services.AddSingleton<JsonDataStore>();

        // Регистрируем наш IFileStorage
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<ISimulatedJsonApiClient, SimulatedJsonApiClient>();

        services.AddScoped<IUnitOfWork, JsonUnitOfWork>();
        services.AddScoped<ISubscriberRepository, JsonSubscriberRepository>();
        services.AddScoped<ICallLogRepository, JsonCallLogRepository>();
        services.AddScoped<ITariffRepository, JsonTariffRepository>();

        return services;
    }
}