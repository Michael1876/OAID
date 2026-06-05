using NUnit.Framework;
using Moq;
using System;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.UI.ViewModels;

namespace AtsBillingSystem.Tests.UI.ViewModels
{
    [TestFixture]
    public class BillingProcessingViewModelTests
    {
        private Mock<IProcessBillingUseCase> _processBillingUseCaseMock;
        private Mock<IDialogService> _dialogServiceMock;
        private BillingProcessingViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _processBillingUseCaseMock = new Mock<IProcessBillingUseCase>();
            _dialogServiceMock = new Mock<IDialogService>();

            _viewModel = new BillingProcessingViewModel(
                _processBillingUseCaseMock.Object,
                _dialogServiceMock.Object);
        }

        [Test]
        public void ExecuteImportAsync_DialogReturnsEmpty_DoesNothing()
        {
            // Arrange
            // Имитируем отмену выбора файла (пользователь закрыл диалог)
            _dialogServiceMock.Setup(x => x.OpenFileDialog(It.IsAny<string>())).Returns(string.Empty);

            // Act
            _viewModel.SelectFileAndExecuteAsyncCommand.Execute(null);

            // Assert
            // Проверяем, что процесс биллинга даже не запускался
            _processBillingUseCaseMock.Verify(
                x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<int>>()),
                Times.Never);

            Assert.That(_viewModel.IsProcessing, Is.False);
        }

        [Test]
        public void ExecuteImportAsync_FileSelectedAndProcessingSucceeds_UpdatesStatusAndShowsMessage()
        {
            // Arrange
            string filePath = "C:\\test\\billing.csv";
            _dialogServiceMock.Setup(x => x.OpenFileDialog(It.IsAny<string>())).Returns(filePath);

            var successResult = new BillingResult { IsSuccess = true };

            // Перехватываем callback прогресса, чтобы убедиться, что он работает
            _processBillingUseCaseMock
                .Setup(x => x.ExecuteAsync(filePath, It.IsAny<Action<int>>()))
                .Callback<string, Action<int>>((path, progressAction) =>
                {
                    // Имитируем обновление прогресса до 50% в процессе работы UseCase
                    progressAction.Invoke(50);
                })
                .ReturnsAsync(successResult);

            // Act
            _viewModel.SelectFileAndExecuteAsyncCommand.Execute(null);

            // Assert
            // 1. Проверяем, что UseCase был вызван с правильным путем файла
            _processBillingUseCaseMock.Verify(x => x.ExecuteAsync(filePath, It.IsAny<Action<int>>()), Times.Once);

            // 2. Убеждаемся, что callback корректно обновил свойство ViewModel
            Assert.That(_viewModel.ProgressBarValue, Is.EqualTo(50));

            // 3. Проверяем итоговое состояние
            Assert.That(_viewModel.IsProcessing, Is.False);
            Assert.That(_viewModel.StatusMessage, Does.Contain("успешно"));

            // 4. Проверяем, что пользователю показали окно с успехом
            _dialogServiceMock.Verify(x => x.ShowMessage("Успех", It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ExecuteImportAsync_ProcessingFails_ShowsErrorMessage()
        {
            // Arrange
            string filePath = "C:\\test\\bad_billing.csv";
            string expectedError = "Файл поврежден";

            _dialogServiceMock.Setup(x => x.OpenFileDialog(It.IsAny<string>())).Returns(filePath);

            var failedResult = new BillingResult
            {
                IsSuccess = false,
                ErrorMessage = expectedError
            };

            _processBillingUseCaseMock
                .Setup(x => x.ExecuteAsync(filePath, It.IsAny<Action<int>>()))
                .ReturnsAsync(failedResult);

            // Act
            _viewModel.SelectFileAndExecuteAsyncCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.IsProcessing, Is.False);
            Assert.That(_viewModel.StatusMessage, Does.Contain("Ошибка"));

            // Проверяем, что показан диалог с сообщением об ошибке из UseCase
            _dialogServiceMock.Verify(x => x.ShowMessage("Ошибка", expectedError), Times.Once);
        }
    }
}