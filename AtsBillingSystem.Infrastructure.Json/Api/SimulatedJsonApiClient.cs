using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AtsBillingSystem.Infrastructure.Json.Dtos;
using AtsBillingSystem.Infrastructure.Json.Mapping;
using AtsBillingSystem.Infrastructure.Json.Storage;

namespace AtsBillingSystem.Infrastructure.Json.Api
{
    public sealed class SimulatedJsonApiClient : ISimulatedJsonApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly JsonApiOptions _options;
        private readonly JsonDataStore _store;
        private readonly SemaphoreSlim _fetchLock = new(1, 1);
        private bool _isLoaded;

        public SimulatedJsonApiClient(JsonApiOptions options, JsonDataStore store)
        {
            _options = options;
            _store = store;
        }

        public string ResolveEndpoint(string resource) =>
            $"{_options.BaseUrl.TrimEnd('/')}/{resource.TrimStart('/')}";

        public Task ReloadAllAsync(CancellationToken cancellationToken = default)
        {
            _isLoaded = false;
            return FetchAllAsync(cancellationToken);
        }

        public async Task FetchAllAsync(CancellationToken cancellationToken = default)
        {
            if (_isLoaded)
                return;

            await _fetchLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_isLoaded)
                    return;

                await SimulateNetworkDelayAsync(cancellationToken).ConfigureAwait(false);

                // Теперь мы ищем файлы напрямую (без папки api/)
                var subscribers = await GetAsync<List<SubscriberJsonDto>>("subscribers.json", cancellationToken)
                    ?? new List<SubscriberJsonDto>();

                await SimulateNetworkDelayAsync(cancellationToken).ConfigureAwait(false);

                var tariffs = await GetAsync<List<TariffJsonDto>>("tariffs.json", cancellationToken)
                    ?? new List<TariffJsonDto>();

                await SimulateNetworkDelayAsync(cancellationToken).ConfigureAwait(false);

                var calls = await GetAsync<List<CallRecordJsonDto>>("call-records.json", cancellationToken)
                    ?? new List<CallRecordJsonDto>();

                _store.ReplaceAll(
                    subscribers.Select(s => s.ToDomain()),
                    tariffs.Select(t => t.ToDomain()),
                    calls.Select(c => c.ToDomain()));

                _isLoaded = true;
            }
            finally
            {
                _fetchLock.Release();
            }
        }

        public async Task PushAllAsync(CancellationToken cancellationToken = default)
        {
            await SimulateNetworkDelayAsync(cancellationToken).ConfigureAwait(false);

            var subscribers = _store.GetSubscribersSnapshot().Select(s => s.ToDto()).ToList();
            await PutAsync("subscribers.json", subscribers, cancellationToken).ConfigureAwait(false);

            await SimulateNetworkDelayAsync(cancellationToken).ConfigureAwait(false);

            var tariffs = _store.GetTariffsSnapshot().Select(t => t.ToDto()).ToList();
            await PutAsync("tariffs.json", tariffs, cancellationToken).ConfigureAwait(false);

            await SimulateNetworkDelayAsync(cancellationToken).ConfigureAwait(false);

            var calls = _store.GetCallRecordsSnapshot().Select(c => c.ToDto()).ToList();
            await PutAsync("call-records.json", calls, cancellationToken).ConfigureAwait(false);
        }

        private async Task<T?> GetAsync<T>(string relativePath, CancellationToken cancellationToken)
        {
            var path = MapToFilePath(relativePath);
            
            if (!File.Exists(path))
            {
                // Теперь, если файл не найден, вы точно это увидите в окне ошибки с указанием точного пути!
                throw new FileNotFoundException($"Файл данных не найден по пути:\n{path}");
            }

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        private async Task PutAsync<T>(string relativePath, T payload, CancellationToken cancellationToken)
        {
            var path = MapToFilePath(relativePath);
            // Убеждаемся, что директория существует
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, payload, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        private string MapToFilePath(string relativePath)
        {
            var normalized = relativePath.Replace('\\', '/').TrimStart('/');
            return Path.Combine(ResolveDataDirectory(), normalized);
        }

        private string ResolveDataDirectory()
        {
            if (Path.IsPathRooted(_options.DataDirectory))
                return _options.DataDirectory;

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, _options.DataDirectory);
                if (Directory.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }

            return Path.Combine(AppContext.BaseDirectory, _options.DataDirectory);
        }

        private async Task SimulateNetworkDelayAsync(CancellationToken cancellationToken)
        {
            if (_options.SimulateNetworkDelayMs <= 0)
                return;

            await Task.Delay(_options.SimulateNetworkDelayMs, cancellationToken).ConfigureAwait(false);
        }
    }
}