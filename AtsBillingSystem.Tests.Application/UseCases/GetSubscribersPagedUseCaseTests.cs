using NUnit.Framework;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;
using AtsBillingSystem.Application.UseCases;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Tests.Application.UseCases
{
    [TestFixture]
    public class GetSubscribersPagedUseCaseTests
    {
        private Mock<ISubscriberRepository> _subscriberRepoMock;
        private GetSubscribersPagedUseCase _useCase;

        [SetUp]
        public void Setup()
        {
            _subscriberRepoMock = new Mock<ISubscriberRepository>();
            _useCase = new GetSubscribersPagedUseCase(_subscriberRepoMock.Object);
        }

        [Test]
        public async Task ExecuteAsync_PageLessThanOne_CorrectsToOne()
        {
            // Arrange
            int invalidPage = 0;
            int validPageSize = 20;
            SetupRepositoryToReturnEmpty(1, validPageSize);

            // Act
            await _useCase.ExecuteAsync(invalidPage, validPageSize);

            // Assert
            // Проверяем, что в репозиторий ушла страница 1, а не 0
            _subscriberRepoMock.Verify(x => x.GetPagedAsync(1, validPageSize), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_PageSizeLessThanOne_CorrectsToTen()
        {
            // Arrange
            int validPage = 1;
            int invalidPageSize = -5;
            SetupRepositoryToReturnEmpty(validPage, 10);

            // Act
            await _useCase.ExecuteAsync(validPage, invalidPageSize);

            // Assert
            // Проверяем, что размер страницы скорректирован до минимального дефолта (10)
            _subscriberRepoMock.Verify(x => x.GetPagedAsync(validPage, 10), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_PageSizeGreaterThan100_CorrectsTo100()
        {
            // Arrange
            int validPage = 2;
            int overlyLargePageSize = 500;
            SetupRepositoryToReturnEmpty(validPage, 100);

            // Act
            await _useCase.ExecuteAsync(validPage, overlyLargePageSize);

            // Assert
            // Защита от перегрузки памяти: не больше 100 записей за раз
            _subscriberRepoMock.Verify(x => x.GetPagedAsync(validPage, 100), Times.Once);
        }

        private void SetupRepositoryToReturnEmpty(int expectedPage, int expectedPageSize)
        {
            var emptyResult = new PagedResult<DomainSubscriber>(new List<DomainSubscriber>(), 0, expectedPage, expectedPageSize);
            _subscriberRepoMock.Setup(x => x.GetPagedAsync(expectedPage, expectedPageSize))
                .ReturnsAsync(emptyResult);
        }
    }
}