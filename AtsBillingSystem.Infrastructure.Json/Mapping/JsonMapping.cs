using System.Text;
using AtsBillingSystem.Domain.Enums;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Infrastructure.Json.Dtos;

namespace AtsBillingSystem.Infrastructure.Json.Mapping;

internal static class JsonMapping
{
    public static DomainSubscriber ToDomain(this SubscriberJsonDto dto) => new()
    {
        Id = dto.Id,
        FullName = dto.FullName,
        ContractNumber = dto.ContractNumber,
        PhoneNumber = dto.PhoneNumber,
        Balance = dto.Balance,
        IsActive = dto.IsActive,
        TariffId = dto.TariffId,
        RowVersion = DecodeRowVersion(dto.RowVersion)
    };

    public static SubscriberJsonDto ToDto(this DomainSubscriber domain) => new()
    {
        Id = domain.Id,
        FullName = domain.FullName,
        ContractNumber = domain.ContractNumber,
        PhoneNumber = domain.PhoneNumber,
        Balance = domain.Balance,
        IsActive = domain.IsActive,
        TariffId = domain.TariffId,
        RowVersion = EncodeRowVersion(domain.RowVersion)
    };

    public static DomainTariff ToDomain(this TariffJsonDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        InternalMinutePrice = dto.InternalMinutePrice,
        CityMinutePrice = dto.CityMinutePrice,
        ConnectionFee = dto.ConnectionFee,
        SubscriptionFee = dto.SubscriptionFee,
        IsArchived = dto.IsArchived
    };

    public static TariffJsonDto ToDto(this DomainTariff domain) => new()
    {
        Id = domain.Id,
        Name = domain.Name,
        InternalMinutePrice = domain.InternalMinutePrice,
        CityMinutePrice = domain.CityMinutePrice,
        ConnectionFee = domain.ConnectionFee,
        SubscriptionFee = domain.SubscriptionFee,
        IsArchived = domain.IsArchived
    };

    public static DomainCallRecord ToDomain(this CallRecordJsonDto dto) => new()
    {
        Id = dto.Id,
        SubscriberId = dto.SubscriberId,
        DestinationNumber = dto.DestinationNumber,
        StartTime = dto.StartTime,
        DurationSeconds = dto.DurationSeconds,
        CallType = dto.CallType,
        Cost = dto.Cost
    };

    public static CallRecordJsonDto ToDto(this DomainCallRecord domain) => new()
    {
        Id = domain.Id,
        SubscriberId = domain.SubscriberId,
        DestinationNumber = domain.DestinationNumber,
        StartTime = domain.StartTime,
        DurationSeconds = domain.DurationSeconds,
        CallType = domain.CallType,
        Cost = domain.Cost
    };

    private static byte[] DecodeRowVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<byte>();

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }

    private static string EncodeRowVersion(byte[] value) =>
        value.Length == 0 ? string.Empty : Convert.ToBase64String(value);
}
