using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Domain.Interfaces.UseCases
{
    public interface IGetSubscribersPagedUseCase
    {
        Task<PagedResult<DomainSubscriber>> ExecuteAsync(int page, int pageSize);
    }

    public interface IAddSubscriberUseCase
    {
        Task ExecuteAsync(DomainSubscriber subscriber);
    }

    public interface IUpdateSubscriberUseCase
    {
        Task ExecuteAsync(DomainSubscriber subscriber);
    }

    public interface IGetCallLogsUseCase
    {
        Task<PagedResult<DomainCallRecord>> ExecuteAsync(DateTime? start, DateTime? end, string phone, int page, int pageSize);
    }

    public interface IProcessBillingUseCase
    {
        // progressCallback нужен для обновления ProgressBar во ViewModel
        Task<BillingResult> ExecuteAsync(string filePath, Action<int> progressCallback);
    }

    public interface IAuthenticateUseCase
    {
        Task<bool> ExecuteAsync(string login, string password);
    }

    public interface IGetActiveTariffsUseCase
    {
        Task<IEnumerable<DomainTariff>> ExecuteAsync();
    }
}

namespace AtsBillingSystem.Domain.Interfaces.Services
{
    public interface IBillingService
    {
        // Вычисляем стоимость конкретного звонка
        decimal CalculateCost(int durationSeconds, Enums.CallType type, DomainTariff tariff);

        // Массовый обсчет CDR без обращения к БД внутри метода (кроме сохранения в конце)
        Task<BillingResult> ProcessCdrBatchAsync(IEnumerable<ParsedCallDto> parsedCalls, Action<int> progressCallback);
    }

    public interface IFileParser
    {
        Task<IEnumerable<ParsedCallDto>> ParseAsync(string filePath);
        bool ValidateFileHash(string filePath);
        void MarkFileAsProcessed(string filePath);
    }

    public interface IAuthService
    {
        Task<bool> AuthenticateAsync(string login, string password);
        string HashPassword(string raw);
    }
}