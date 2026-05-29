namespace AtsBillingSystem.Infrastructure.Json;

public sealed class JsonApiOptions
{
    /// <summary>Базовый URL «сервера» (для логов и симуляции).</summary>
    public string BaseUrl { get; set; } = "https://api.ats-billing.local/";

    /// <summary>Папка с JSON-файлами, имитирующими ответы API.</summary>
    public string DataDirectory { get; set; } = "MockApiData";

    /// <summary>Задержка при каждом «сетевом» запросе, мс.</summary>
    public int SimulateNetworkDelayMs { get; set; } = 300;
}
