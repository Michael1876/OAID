using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.UI.Infrastructure;

namespace AtsBillingSystem.UI.ViewModels
{
    public class SubscribersViewModel : ViewModelBase
    {
        // ВНИМАНИЕ: Подставь сюда свой реальный интерфейс из файла Interfaces/UseCases.cs
        private readonly IGetSubscribersPagedUseCase _getSubscribersUseCase;

        // ObservableCollection сама уведомляет UI о добавлении/удалении элементов
        public ObservableCollection<DomainSubscriber> Subscribers { get; } = new();

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public AsyncRelayCommand LoadPageCommand { get; }
        public AsyncRelayCommand NextPageCommand { get; }
        public AsyncRelayCommand PreviousPageCommand { get; }

        public SubscribersViewModel(IGetSubscribersPagedUseCase getSubscribersUseCase)
        {
            _getSubscribersUseCase = getSubscribersUseCase;

            LoadPageCommand = new AsyncRelayCommand(LoadSubscribersAsync);

            NextPageCommand = new AsyncRelayCommand(
                async () => { CurrentPage++; await LoadSubscribersAsync(); },
                () => CurrentPage < TotalPages && !IsLoading);

            PreviousPageCommand = new AsyncRelayCommand(
                async () => { CurrentPage--; await LoadSubscribersAsync(); },
                () => CurrentPage > 1 && !IsLoading);

            // Инициируем первичную загрузку (в реальном проекте лучше делать это через события навигации, 
            // но для простоты вызываем асинхронную команду прямо здесь без await)
            LoadPageCommand.Execute(null);
        }

        private async Task LoadSubscribersAsync()
        {
            try
            {
                IsLoading = true;
                Subscribers.Clear();

                var pagedResult = await _getSubscribersUseCase.ExecuteAsync(CurrentPage, pageSize: 50);

                if (pagedResult != null)
                {
                    TotalPages = (int)Math.Ceiling((double)pagedResult.TotalCount / pagedResult.PageSize);
                    if (TotalPages == 0) TotalPages = 1;

                    foreach (var subscriber in pagedResult.Items)
                        Subscribers.Add(subscriber);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка загрузки абонентов:\n{ex.Message}",
                    "Абоненты",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}