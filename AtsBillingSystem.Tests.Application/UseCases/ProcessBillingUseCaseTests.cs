using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AtsBillingSystem.Application.UseCases;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.Domain.Interfaces.Services;

namespace AtsBillingSystem.Tests.Application.UseCases
{
    [TestFixture]
    public class ProcessBillingUseCaseTests
    {
        private Mock<IFileParser> _fileParserMock;
        private Mock<IBillingService> _billingServiceMock;
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private ProcessBillingUseCase _useCase;

        [SetUp]
        public void Setup()
        {
            _fileParserMock = new Mock<IFileParser>();
            _billingServiceMock = new Mock<IBillingService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _useCase = new ProcessBillingUseCase(
                _fileParserMock.Object,
                _billingServiceMock.Object,
                _unitOfWorkMock.Object);
        }

        [Test]
        public async Task ExecuteAsync_InvalidFileHash_ReturnsErrorImmediatelyAndDoesNotStartTransaction()
        {
            // Arrange
            var filePath = "dummy.csv";
            _fileParserMock.Setup(x => x.ValidateFileHash(filePath)).Returns(false);

            // Act
            var result = await _useCase.ExecuteAsync(filePath, null);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Файл поврежден или уже был обработан"));

            // Строгая проверка: убеждаемся, что мы даже не пытались читать файл и открывать транзакцию
            _fileParserMock.Verify(x => x.ParseAsync(It.IsAny<string>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ProcessingSucceeds_CommitsTransaction()
        {
            // Arrange
            var filePath = "valid.csv";
            _fileParserMock.Setup(x => x.ValidateFileHash(filePath)).Returns(true);
            _fileParserMock.Setup(x => x.ParseAsync(filePath))
                .ReturnsAsync(new List<ParsedCallDto>());

            _billingServiceMock.Setup(x => x.ProcessCdrBatchAsync(It.IsAny<IEnumerable<ParsedCallDto>>(), It.IsAny<Action<int>>()))
                .ReturnsAsync(new BillingResult { IsSuccess = true });

            // Act
            var result = await _useCase.ExecuteAsync(filePath, null);

            // Assert
            Assert.That(result.IsSuccess, Is.True);

            // Проверка строгого порядка: Begin -> Process -> SaveChanges -> Commit
            _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ExceptionThrownDuringProcessing_RollsBackTransaction()
        {
            // Arrange
            var filePath = "valid.csv";
            _fileParserMock.Setup(x => x.ValidateFileHash(filePath)).Returns(true);
            _fileParserMock.Setup(x => x.ParseAsync(filePath))
                .ReturnsAsync(new List<ParsedCallDto>());

            // Имитируем падение БД при попытке сохранить данные (например, DbUpdateConcurrencyException)
            _billingServiceMock.Setup(x => x.ProcessCdrBatchAsync(It.IsAny<IEnumerable<ParsedCallDto>>(), It.IsAny<Action<int>>()))
                .ReturnsAsync(new BillingResult { IsSuccess = true });

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
                .ThrowsAsync(new Exception("Database connection lost"));

            // Act
            var result = await _useCase.ExecuteAsync(filePath, null);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Ошибка при обработке биллинга"));

            // Проверяем, что транзакция БЫЛА начата, НЕ БЫЛА закоммичена, но БЫЛА откатана
            _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Never);
            _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
    }
}