using System;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Interfaces.UseCases;

namespace AtsBillingSystem.Application.UseCases
{
    public class GetSubscribersPagedUseCase : IGetSubscribersPagedUseCase
    {
        private readonly ISubscriberRepository _subscriberRepository;

        public GetSubscribersPagedUseCase(ISubscriberRepository subscriberRepository)
        {
            _subscriberRepository = subscriberRepository ?? throw new ArgumentNullException(nameof(subscriberRepository));
        }

        public async Task<PagedResult<DomainSubscriber>> ExecuteAsync(int page, int pageSize)
        {
            // Валидация входных параметров (Guard Clauses) на уровне бизнес-логики.
            // UI может прислать некорректные данные, но UseCase — это последний рубеж защиты.
            if (page < 1) page = 1;

            // Задаем разумные границы размера страницы, чтобы избежать перегрузки приложения
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Делегируем выполнение эффективного SQL-запроса в слой Data через интерфейс репозитория
            return await _subscriberRepository.GetPagedAsync(page, pageSize);
        }
    }
}