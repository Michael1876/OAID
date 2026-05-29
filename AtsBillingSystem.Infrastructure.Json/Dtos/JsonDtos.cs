using System.Text.Json.Serialization;
using AtsBillingSystem.Domain.Enums;

namespace AtsBillingSystem.Infrastructure.Json.Dtos;

internal sealed class SubscriberJsonDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public Guid TariffId { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

internal sealed class TariffJsonDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal InternalMinutePrice { get; set; }
    public decimal CityMinutePrice { get; set; }
    public decimal ConnectionFee { get; set; }
    public decimal SubscriptionFee { get; set; }
    public bool IsArchived { get; set; }
}

internal sealed class CallRecordJsonDto
{
    public Guid Id { get; set; }
    public Guid SubscriberId { get; set; }
    public string DestinationNumber { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int DurationSeconds { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CallType CallType { get; set; }

    public decimal Cost { get; set; }
}
