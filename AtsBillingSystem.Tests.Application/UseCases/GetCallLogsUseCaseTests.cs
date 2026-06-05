using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AtsBillingSystem.Application.UseCases;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Tests.Application.UseCases
{
    [TestFixture]
    public class GetCallLogsUseCaseTests
    {
        private Mock<ICallLogRepository> _callLogRepoMock;
        private GetCallLogsUseCase _useCase;

        [SetUp]
        public void Setup()
        {
            _callLogRepoMock = new Mock<ICallLogRepository>();
            _useCase = new GetCallLogsUseCase(_callLogRepoMock.Object);
        }

        [Test]
        public async Task ExecuteAsync_PageLessThanOne_CorrectsToOne()
        {
            // Arrange
            var invalidPage = 0;
            var validPageSize = 50;
            SetupRepositoryMock(1, validPageSize);

            // Act
            await _useCase.ExecuteAsync(null, null, null, invalidPage, validPageSize);

            // Assert
            _callLogRepoMock.Verify(x => x.GetByFilterAsync(null, null, string.Empty, 1, validPageSize), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_PageSizeOutOfBoundaries_CorrectsToDefaults()
        {
            // Arrange
            var validPage = 1;
            var negativePageSize = -5;
            var tooLargePageSize = 200;

            // Настраиваем мок, чтобы он не падал при вызовах
            SetupRepositoryMock(validPage, 10);
            SetupRepositoryMock(validPage, 100);

            // Act
            await _useCase.ExecuteAsync(null, null, null, validPage, negativePageSize);
            await _useCase.ExecuteAsync(null, null, null, validPage, tooLargePageSize);

            // Assert
            // Проверяем, что отрицательный размер скорректировался до 10
            _callLogRepoMock.Verify(x => x.GetByFilterAsync(null, null, string.Empty, validPage, 10), Times.Once);
            // Проверяем, что слишком большой размер срезался до 100
            _callLogRepoMock.Verify(x => x.GetByFilterAsync(null, null, string.Empty, validPage, 100), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_NullPhoneFilter_PassesEmptyStringToRepository()
        {
            // Arrange
            // Важно: репозитории часто плохо переваривают null в строковых фильтрах (особенно в EF Core .Contains).
            // Юзкейс должен обезопасить слой данных, заменив null на string.Empty.
            SetupRepositoryMock(1, 20);

            // Act
            await _useCase.ExecuteAsync(DateTime.Now, DateTime.Now, null, 1, 20);

            // Assert
            _callLogRepoMock.Verify(x => x.GetByFilterAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), string.Empty, 1, 20), Times.Once);
        }

        private void SetupRepositoryMock(int expectedPage, int expectedPageSize)
        {
            var emptyResult = new PagedResult<DomainCallRecord>(new List<DomainCallRecord>(), 0, expectedPage, expectedPageSize);
            _callLogRepoMock.Setup(x => x.GetByFilterAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), expectedPage, expectedPageSize))
                .ReturnsAsync(emptyResult);
        }
    }
}