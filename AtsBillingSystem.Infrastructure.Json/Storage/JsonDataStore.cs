using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Infrastructure.Json.Storage;

/// <summary>Локальный кэш данных после «загрузки с сервера» и до POST-сохранения.</summary>
public sealed class JsonDataStore
{
    private readonly object _sync = new();

    public List<DomainSubscriber> Subscribers { get; } = new();
    public List<DomainTariff> Tariffs { get; } = new();
    public List<DomainCallRecord> CallRecords { get; } = new();

    public void ReplaceAll(
        IEnumerable<DomainSubscriber> subscribers,
        IEnumerable<DomainTariff> tariffs,
        IEnumerable<DomainCallRecord> callRecords)
    {
        lock (_sync)
        {
            Subscribers.Clear();
            Subscribers.AddRange(subscribers);

            Tariffs.Clear();
            Tariffs.AddRange(tariffs);

            CallRecords.Clear();
            CallRecords.AddRange(callRecords);
        }
    }

    public IReadOnlyList<DomainSubscriber> GetSubscribersSnapshot()
    {
        lock (_sync)
            return Subscribers.ToList();
    }

    public IReadOnlyList<DomainTariff> GetTariffsSnapshot()
    {
        lock (_sync)
            return Tariffs.ToList();
    }

    public IReadOnlyList<DomainCallRecord> GetCallRecordsSnapshot()
    {
        lock (_sync)
            return CallRecords.ToList();
    }

    public DomainSubscriber? FindSubscriber(Guid id)
    {
        lock (_sync)
            return Subscribers.FirstOrDefault(s => s.Id == id);
    }

    public void UpdateSubscriber(DomainSubscriber subscriber)
    {
        lock (_sync)
        {
            var index = Subscribers.FindIndex(s => s.Id == subscriber.Id);
            if (index >= 0)
                Subscribers[index] = subscriber;
        }
    }

    public void UpdateBalances(IEnumerable<DomainSubscriber> subscribers)
    {
        lock (_sync)
        {
            foreach (var updated in subscribers)
            {
                var index = Subscribers.FindIndex(s => s.Id == updated.Id);
                if (index >= 0)
                    Subscribers[index] = updated;
            }
        }
    }

    public void AddCallRecords(IEnumerable<DomainCallRecord> records)
    {
        lock (_sync)
            CallRecords.AddRange(records);
    }

    public void ArchiveTariff(Guid id)
    {
        lock (_sync)
        {
            var tariff = Tariffs.FirstOrDefault(t => t.Id == id);
            if (tariff != null)
                tariff.IsArchived = true;
        }
    }
}
