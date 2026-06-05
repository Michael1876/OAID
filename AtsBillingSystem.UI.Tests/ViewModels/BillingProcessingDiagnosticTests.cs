using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.UI.ViewModels;

namespace AtsBillingSystem.Tests.UI.ViewModels
{
    [TestFixture]
    public class BillingProcessingDiagnosticTests
    {
        private Mock<IProcessBillingUseCase> _useCaseMock;
        private Mock<IDialogService> _dialogServiceMock;
        private BillingProcessingViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _useCaseMock = new Mock<IProcessBillingUseCase>();
            _dialogServiceMock = new Mock<IDialogService>();

            _viewModel = new BillingProcessingViewModel(
                _useCaseMock.Object,
                _dialogServiceMock.Object);
        }

        [Test]
        public void Diagnostic_Step1_Command_CanExecute_Initially_True()
        {
            // Проверяем, разрешает ли команда свое выполнение. 
            // Если этот тест упадет — WPF кнопка визуально станет серой или не будет реагировать на клик.
            bool canExecute = _viewModel.SelectFileAndExecuteAsyncCommand.CanExecute(null);

            Assert.That(canExecute, Is.True,
                "КРИТИЧЕСКИЙ СБОЙ: Команда SelectFileAndExecuteAsyncCommand заблокирована изначально! Кнопка в UI не нажмется.");
        }

        [Test]
        public async Task Diagnostic_Step2_DialogService_ReturnsEmpty_ResetsProcessingFlag()
        {
            // Имитируем ситуацию, когда диалоговое окно выбора файла открылось, но пользователь нажал "Отмена".
            _dialogServiceMock.Setup(x => x.OpenFileDialog(It.IsAny<string>())).Returns(string.Empty);

            // Вызываем команду
            _viewModel.SelectFileAndExecuteAsyncCommand.Execute(null);

            // Даем async void методу внутри команды 50мс на завершение
            await Task.Delay(50);

            // Проверяем, не завис ли флаг загрузки
            Assert.That(_viewModel.IsProcessing, Is.False,
                "СБОЙ ПОТОКА: Флаг IsProcessing остался true после отмены выбора файла! Кнопка заблокировалась навсегда.");

            _useCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<int>>()), Times.Never,
                "ОШИБКА: Запущен процесс обработки пустого пути файла!");
        }

        [Test]
        public async Task Diagnostic_Step3_UseCase_ThrowsException_CapturedByViewModel()
        {
            // Проверяем устойчивость к системным ошибкам (например, файл CSV заблокирован другой программой).
            string filePath = "locked_file.csv";
            _dialogServiceMock.Setup(x => x.OpenFileDialog(It.IsAny<string>())).Returns(filePath);

            // Имитируем жесткое падение внутри слоя Application
            _useCaseMock.Setup(x => x.ExecuteAsync(filePath, It.IsAny<Action<int>>()))
                .ThrowsAsync(new Exception("Файл заблокирован операционной системой"));

            _viewModel.SelectFileAndExecuteAsyncCommand.Execute(null);
            await Task.Delay(50);

            // Флаг должен сброситься в finally, а пользователю должно показаться сообщение
            Assert.That(_viewModel.IsProcessing, Is.False,
                "ФАТАЛЬНО: Исключение привело к зависанию флага IsProcessing в состоянии true!");

            _dialogServiceMock.Verify(x => x.ShowMessage(It.Is<string>(t => t.Contains("ошибка") || t.Contains("Критическая")), It.IsAny<string>()), Times.Once,
                "ОШИБКА АРХИТЕКТУРЫ: ViewModel «проглотила» ошибку молча. В UI ничего не произошло, логов нет.");
        }

        [Test]
        public async Task Diagnostic_Step4_UseCase_ReturnsFailure_UpdatesStatusMessage()
        {
            // Проверяем, как ViewModel реагирует на штатный отказ бизнес-логики (например, хэш дублируется)
            string filePath = "processed.csv";
            _dialogServiceMock.Setup(x => x.OpenFileDialog(It.IsAny<string>())).Returns(filePath);

            var failedResult = new BillingResult
            {
                IsSuccess = false,
                ErrorMessage = "Этот файл уже был обработан ранее."
            };

            _useCaseMock.Setup(x => x.ExecuteAsync(filePath, It.IsAny<Action<int>>())).ReturnsAsync(failedResult);

            _viewModel.SelectFileAndExecuteAsyncCommand.Execute(null);
            await Task.Delay(50);

            Assert.That(_viewModel.StatusMessage, Is.EqualTo("Ошибка импорта."),
                "ОШИБКА UI: Текст StatusMessage не обновился при неудачном биллинге.");

            _dialogServiceMock.Verify(x => x.ShowMessage("Внимание", failedResult.ErrorMessage), Times.Once,
                "ОШИБКА UI: Пользователю не вывелось окно предупреждения с текстом ошибки из UseCase.");
        }

        [Test]
        public async Task Diagnostic_Step5_SuccessPath_WithFailedLines_PopulatesCollections()
        {
            // Проверяем комбинированный сценарий: файл успешно зачитан, но внутри есть битые строки
            string filePath = "data.csv";
            _dialogServiceMock.Setup(x => x.OpenFileDialog(It.IsAny<string>())).Returns(filePath);

            var mixedResult = new BillingResult
            {
                IsSuccess = true,
                FailedItems = new List<string> { "Линия 4: Абонент не найден." }
            };

            _useCaseMock.Setup(x => x.ExecuteAsync(filePath, It.IsAny<Action<int>>())).ReturnsAsync(mixedResult);

            _viewModel.SelectFileAndExecuteAsyncCommand.Execute(null);
            await Task.Delay(50);

            Assert.That(_viewModel.HasErrors, Is.True,
                "ОШИБКА ЛОГИКИ: Флаг HasErrors равен false, хотя в результатах есть пропущенные строки звонков.");

            Assert.That(_viewModel.FailedItems, Has.Count.EqualTo(1),
                "ОШИБКА СВЯЗЫВАНИЯ: Коллекция FailedItems осталась пустой, данные не дошли до элементов отображения UI.");

            Assert.That(_viewModel.FailedItems[0], Is.EqualTo("Линия 4: Абонент не найден."));
        }
    }
}