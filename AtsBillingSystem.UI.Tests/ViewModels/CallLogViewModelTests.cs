using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.UI.ViewModels;

namespace AtsBillingSystem.Tests.UI.ViewModels
{
    [TestFixture]
    public class CallLogViewModelTests
    {
        private Mock<IGetCallLogsUseCase> _useCaseMock;
        private Mock<ILogger<CallLogViewModel>> _loggerMock;
        private Mock<IDialogService> _dialogServiceMock;
        private CallLogViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _useCaseMock = new Mock<IGetCallLogsUseCase>();
            _loggerMock = new Mock<ILogger<CallLogViewModel>>();
            _dialogServiceMock = new Mock<IDialogService>();

            // Настраиваем UseCase так, чтобы он возвращал пустой результат по умолчанию
            var emptyResult = new PagedResult<DomainCallRecord>(new List<DomainCallRecord>(), 0, 1, 50);

            _useCaseMock.Setup(x => x.ExecuteAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(emptyResult);

            _viewModel = new CallLogViewModel(
                _useCaseMock.Object,
                _loggerMock.Object,
                _dialogServiceMock.Object);
        }

        // =========================================================================================
        // ДИАГНОСТИЧЕСКИЙ БЛОК: ТЕСТЫ ДЛЯ ПОИСКА ТОЧНОЙ ПРИЧИНЫ "ПУСТОГО ЭКРАНА"
        // =========================================================================================

        [Test]
        public async Task Diagnostic_Step1_Constructor_MustInvokeUseCase_WithoutCrashing()
        {
            // Даем фоновому потоку время отработать (300 мс хватит за глаза)
            await Task.Delay(300);

            // Assert 1: Проверяем, не выпала ли тихая ошибка внутри LoadCallLogsAsync. 
            // Если этот ассерт упадет, значит код падает ДО обращения к UseCase.
            _dialogServiceMock.Verify(
                x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never,
                "АХТУНГ: Внутри LoadCallLogsAsync выпало исключение! Оно было перехвачено и отправлено в диалог. Посмотри в дебаггер.");

            // Assert 2: Проверяем, что метод UseCase был вызван с ЛЮБЫМИ параметрами.
            // Если этот ассерт упадет, значит вызов _ = LoadCallLogsAsync(1) вообще отсутствует в конструкторе,
            // либо срабатывает ранний выход if (IsLoading) return;
            _useCaseMock.Verify(x => x.ExecuteAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Once,
                "АХТУНГ: ExecuteAsync вообще не был вызван! Проверь, есть ли вызов _ = LoadCallLogsAsync(1) в конце конструктора CallLogViewModel.");
        }

        [Test]
        public async Task Diagnostic_Step2_CheckStateLock_IsLoadingMustBeFalseAfterInit()
        {
            await Task.Delay(300);

            // Если этот тест падает, значит IsLoading залип в true. Из-за этого команды не работают 
            // и UI бесконечно показывает надпись "Загрузка...". Ошибка кроется в блоке finally.
            Assert.That(_viewModel.IsLoading, Is.False, "АХТУНГ: Флаг IsLoading завис в состоянии true после инициализации!");
        }

        [Test]
        public async Task Diagnostic_Step3_ApplyFilterCommand_MustTriggerUseCase()
        {
            // Сбрасываем историю вызовов мока, чтобы исключить фоновый вызов из конструктора
            _useCaseMock.Invocations.Clear();

            // Act
            _viewModel.FilterStartDate = new DateTime(2025, 05, 01);
            _viewModel.SearchPhoneNumber = "74951234501";
            _viewModel.ApplyFilterCommand.Execute(null);

            await Task.Delay(300);

            // Assert
            // Проверяем, что команда смогла пробиться через IsLoading и отправить данные
            _useCaseMock.Verify(x => x.ExecuteAsync(
                It.Is<DateTime?>(d => d.HasValue && d.Value.Year == 2025),
                It.IsAny<DateTime?>(),
                It.Is<string>(s => s == "74951234501"),
                It.Is<int>(p => p == 1),
                It.IsAny<int>()),
                Times.Once,
                "АХТУНГ: Команда ApplyFilterCommand нажата, но данные не пошли в UseCase. Возможно, кнопка заблокирована (CanExecute).");
        }

        // =========================================================================================
        // СТАНДАРТНЫЕ ТЕСТЫ ПОВЕДЕНИЯ (BUSINESS LOGIC)
        // =========================================================================================

        [Test]
        public async Task ApplyFilterCommand_ResetsToFirstPage_WhenExecuted()
        {
            // Arrange
            _viewModel.CurrentPage = 3;
            _useCaseMock.Invocations.Clear(); // Очищаем кэш конструктора

            // Act
            _viewModel.ApplyFilterCommand.Execute(null);
            await Task.Delay(300);

            // Assert
            Assert.That(_viewModel.CurrentPage, Is.EqualTo(1), "Применение нового фильтра должно сбрасывать пагинацию на 1 страницу.");
        }

        [Test]
        public async Task NextPageCommand_CannotExecute_WhenOnLastPage()
        {
            // Arrange
            var onePageResult = new PagedResult<DomainCallRecord>(new List<DomainCallRecord>(), 10, 1, 50);
            _useCaseMock.Setup(x => x.ExecuteAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(onePageResult);

            _viewModel.ApplyFilterCommand.Execute(null);
            await Task.Delay(300);

            // Act
            bool canGoNext = _viewModel.NextPageCommand.CanExecute(null);

            // Assert
            Assert.That(_viewModel.TotalPages, Is.EqualTo(1));
            Assert.That(canGoNext, Is.False, "Кнопка 'Вперед' должна быть заблокирована, если мы на последней странице.");
        }

        [Test]
        public async Task LoadCallLogs_FillsCollection_Correctly()
        {
            // Arrange
            var records = new List<DomainCallRecord>
            {
                new DomainCallRecord { Id = Guid.NewGuid(), DestinationNumber = "111" },
                new DomainCallRecord { Id = Guid.NewGuid(), DestinationNumber = "222" }
            };
            var result = new PagedResult<DomainCallRecord>(records, 2, 1, 50);

            _useCaseMock.Setup(x => x.ExecuteAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(result);

            // Act
            _viewModel.ApplyFilterCommand.Execute(null);
            await Task.Delay(300);

            // Assert
            Assert.That(_viewModel.Calls, Has.Count.EqualTo(2), "Коллекция Calls не заполнилась данными!");
            Assert.That(_viewModel.Calls.First().DestinationNumber, Is.EqualTo("111"));
        }

        [Test]
        public async Task LoadCallLogs_ThrowsException_ShowsDialogAndLogsError()
        {
            // Arrange
            var expectedException = new Exception("Test Database Timeout");

            _useCaseMock.Setup(x => x.ExecuteAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(expectedException);

            // Act
            _viewModel.ApplyFilterCommand.Execute(null);
            await Task.Delay(300);

            // Assert
            _dialogServiceMock.Verify(x => x.ShowMessage("Ошибка", It.Is<string>(msg => msg.Contains("Test Database Timeout"))), Times.Once);
            Assert.That(_viewModel.IsLoading, Is.False, "IsLoading не был сброшен в false после ошибки!");
        }
    }
}