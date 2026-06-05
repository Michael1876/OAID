using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AtsBillingSystem.Application.Services;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Enums;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Tests.Application
{
    [TestFixture]
    public class BillingServiceTests
    {
        private Mock<ISubscriberRepository> _subscriberRepoMock;
        private Mock<ITariffRepository> _tariffRepoMock;
        private Mock<ICallLogRepository> _callLogRepoMock;
        private BillingService _billingService;

        [SetUp]
        public void Setup()
        {
            // Пересоздаем моки перед каждым тестом, чтобы исключить влияние тестов друг на друга
            _subscriberRepoMock = new Mock<ISubscriberRepository>();
            _tariffRepoMock = new Mock<ITariffRepository>();
            _callLogRepoMock = new Mock<ICallLogRepository>();

            _billingService = new BillingService(
                _subscriberRepoMock.Object,
                _tariffRepoMock.Object,
                _callLogRepoMock.Object);
        }

        [Test]
        public void CalculateCost_Duration3SecondsOrLess_ReturnsZero()
        {
            // Arrange
            var tariff = new DomainTariff { InternalMinutePrice = 1.5m, ConnectionFee = 10m };

            // Act
            var cost1 = _billingService.CalculateCost(1, CallType.Internal, tariff);
            var cost3 = _billingService.CalculateCost(3, CallType.Internal, tariff);

            // Assert
            Assert.That(cost1, Is.EqualTo(0m));
            Assert.That(cost3, Is.EqualTo(0m));
        }

        [Test]
        public void CalculateCost_InternalCall_AppliesInternalPriceAndConnectionFee()
        {
            // Arrange
            var tariff = new DomainTariff { InternalMinutePrice = 1.5m, ConnectionFee = 1m };
            int durationSeconds = 90; // 1.5 минуты

            // Act: (1.5 минуты * 1.5 цена) + 1 плата за соединение = 2.25 + 1 = 3.25
            var cost = _billingService.CalculateCost(durationSeconds, CallType.Internal, tariff);

            // Assert
            Assert.That(cost, Is.EqualTo(3.25m));
        }

        [Test]
        public void CalculateCost_CityCall_AppliesCityPriceAndRoundsToTwoDecimals()
        {
            // Arrange
            var tariff = new DomainTariff { CityMinutePrice = 3.33m, ConnectionFee = 0m };
            int durationSeconds = 40; // 40/60 = 0.666... минут

            // Act: 0.666... * 3.33 = 2.2199... -> должно округлиться до 2.22
            var cost = _billingService.CalculateCost(durationSeconds, CallType.City, tariff);

            // Assert
            Assert.That(cost, Is.EqualTo(2.22m));
        }

        [Test]
        public async Task ProcessCdrBatchAsync_SubscriberNotFound_AddsToFailedItems()
        {
            // Arrange
            var parsedCalls = new List<ParsedCallDto>
            {
                new() { CallerPhone = "79990001122", ReceiverPhone = "79991112233", DurationSeconds = 60 }
            };

            // Мокаем так, чтобы репозиторий вернул пустой словарь (абоненты не найдены)
            _subscriberRepoMock.Setup(x => x.GetByPhonesBatchAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, DomainSubscriber>());

            _tariffRepoMock.Setup(x => x.GetActiveAsync())
                .ReturnsAsync(new List<DomainTariff>());

            // Act
            var result = await _billingService.ProcessCdrBatchAsync(parsedCalls, null);

            // Assert
            Assert.That(result.IsSuccess, Is.True); // Сам процесс прошел без Exception
            Assert.That(result.FailedItems, Has.Count.EqualTo(1));
            Assert.That(result.FailedItems.First(), Does.Contain("не найден"));

            // Убеждаемся, что ничего не сохранялось
            _callLogRepoMock.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<DomainCallRecord>>()), Times.Never);
        }

        [Test]
        public async Task ProcessCdrBatchAsync_ValidCall_DebitsBalanceAndCallsRepositories()
        {
            // Arrange
            var tariffId = Guid.NewGuid();
            var callerPhone = "79990001111"; // Одинаковый префикс = Internal
            var receiverPhone = "79990002222";

            var parsedCalls = new List<ParsedCallDto>
            {
                new() { CallerPhone = callerPhone, ReceiverPhone = receiverPhone, DurationSeconds = 60 }
            };

            var subscriber = new DomainSubscriber
            {
                Id = Guid.NewGuid(),
                PhoneNumber = callerPhone,
                Balance = 100m,
                TariffId = tariffId
            };

            var activeTariffs = new List<DomainTariff>
            {
                new() { Id = tariffId, InternalMinutePrice = 2m, ConnectionFee = 0m } // Стоимость будет 2m
            };

            _subscriberRepoMock.Setup(x => x.GetByPhonesBatchAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, DomainSubscriber> { { callerPhone, subscriber } });

            _tariffRepoMock.Setup(x => x.GetActiveAsync())
                .ReturnsAsync(activeTariffs);

            // Act
            var result = await _billingService.ProcessCdrBatchAsync(parsedCalls, null);

            // Assert
            Assert.That(result.FailedItems, Is.Empty);
            Assert.That(subscriber.Balance, Is.EqualTo(98m)); // 100 - 2

            // Проверяем, что методы репозиториев были вызваны один раз с нужными данными
            _callLogRepoMock.Verify(x => x.AddRangeAsync(It.Is<IEnumerable<DomainCallRecord>>(
                logs => logs.Count() == 1 && logs.First().Cost == 2m)), Times.Once);

            _subscriberRepoMock.Verify(x => x.UpdateBalancesBatchAsync(It.Is<IEnumerable<DomainSubscriber>>(
                subs => subs.Count() == 1 && subs.First().Id == subscriber.Id)), Times.Once);
        }
    }
}