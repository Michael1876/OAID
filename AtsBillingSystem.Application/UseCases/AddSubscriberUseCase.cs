using System;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Application.UseCases
{
    public class AddSubscriberUseCase : IAddSubscriberUseCase
    {
        private readonly ISubscriberRepository _subscriberRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddSubscriberUseCase(ISubscriberRepository subscriberRepository, IUnitOfWork unitOfWork)
        {
            _subscriberRepository = subscriberRepository ?? throw new ArgumentNullException(nameof(subscriberRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(DomainSubscriber subscriber)
        {
            if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));

            // Защита уровня бизнес-логики (Guard Clauses)
            if (string.IsNullOrWhiteSpace(subscriber.FullName))
            {
                throw new ArgumentException("ФИО абонента не может быть пустым.");
            }

            if (string.IsNullOrWhiteSpace(subscriber.PhoneNumber))
            {
                throw new ArgumentException("Номер телефона абонента не может быть пустым.");
            }

            if (subscriber.TariffId == Guid.Empty)
            {
                throw new ArgumentException("Необходимо выбрать тарифный план для абонента.");
            }

            // Генерируем ID, если это новый абонент
            if (subscriber.Id == Guid.Empty)
            {
                subscriber.Id = Guid.NewGuid();
            }

            await _subscriberRepository.AddAsync(subscriber);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}