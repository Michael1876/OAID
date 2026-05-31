using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Linq;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.UI.ViewModels;

namespace AtsBillingSystem.Tests.UI.ViewModels
{
    [TestFixture]
    public class SubscribersViewModelTests
    {
        private Mock<IGetSubscribersPagedUseCase> _useCaseMock;
        private SubscribersViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _useCaseMock = new Mock<IGetSubscribersPagedUseCase>();

            // Настраиваем дефолтный ответ для первичной загрузки в конструкторе
            var emptyResult = new PagedResult<DomainSubscriber>(new List<DomainSubscriber>(), 0, 1, 50);
            _useCaseMock.Setup(x => x.ExecuteAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(emptyResult);

            _viewModel = new SubscribersViewModel(_useCaseMock.Object);
        }

        [Test]
        public void Constructor_InvokesLoadCommand_FetchesFirstPage()
        {
            // Assert
            // Проверяем, что конструктор действительно инициировал загрузку первой страницы
            _useCaseMock.Verify(x => x.ExecuteAsync(1, 50), Times.Once);
        }

        [Test]
        public void LoadPageCommand_PopulatesSubscribersCollection()
        {
            // Arrange
            var subscribersList = new List<DomainSubscriber>
            {
                new DomainSubscriber { Id = System.Guid.NewGuid(), FullName = "Иван Иванов" },
                new DomainSubscriber { Id = System.Guid.NewGuid(), FullName = "Петр Петров" }
            };

            var pagedResult = new PagedResult<DomainSubscriber>(subscribersList, 2, 1, 50);

            // Перенастраиваем мок, чтобы он вернул конкретные данные
            _useCaseMock.Setup(x => x.ExecuteAsync(1, 50)).ReturnsAsync(pagedResult);

            // Act
            _viewModel.LoadPageCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Subscribers, Has.Count.EqualTo(2));
            Assert.That(_viewModel.Subscribers.First().FullName, Is.EqualTo("Иван Иванов"));
            Assert.That(_viewModel.TotalPages, Is.EqualTo(1));
        }

        [Test]
        public void NextPageCommand_IncrementsPageAndLoadsData()
        {
            // Arrange
            _viewModel.TotalPages = 5; // Имитируем, что у нас много страниц
            _viewModel.CurrentPage = 1;

            // Act
            _viewModel.NextPageCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.CurrentPage, Is.EqualTo(2));
            // Метод UseCase вызовется дважды: 1 раз в конструкторе (стр. 1), 1 раз здесь (стр. 2)
            _useCaseMock.Verify(x => x.ExecuteAsync(2, 50), Times.Once);
        }

        [Test]
        public void PreviousPageCommand_DecrementsPageAndLoadsData()
        {
            // Arrange
            _viewModel.TotalPages = 5;
            _viewModel.CurrentPage = 3; // Имитируем, что мы на 3 странице

            // Act
            _viewModel.PreviousPageCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.CurrentPage, Is.EqualTo(2));
            _useCaseMock.Verify(x => x.ExecuteAsync(2, 50), Times.Once);
        }
    }
}