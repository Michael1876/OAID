using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.UI.Infrastructure;
using System.Linq;

namespace AtsBillingSystem.UI.ViewModels
{
    public class CallLogViewModel : ViewModelBase
    {
        private readonly IGetCallLogsUseCase _getCallLogsUseCase;
        private readonly ILogger<CallLogViewModel> _logger;
        private readonly IDialogService _dialogService;
        private readonly int _pageSize = 50;
        private bool _isInitialized = false;

        public ObservableCollection<DomainCallRecord> Calls { get; } = new();

        private DateTime? _filterStartDate;
        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set => SetProperty(ref _filterStartDate, value);
        }

        private DateTime? _filterEndDate;
        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set => SetProperty(ref _filterEndDate, value);
        }

        private string _searchPhoneNumber = string.Empty;
        public string SearchPhoneNumber
        {
            get => _searchPhoneNumber;
            set => SetProperty(ref _searchPhoneNumber, value);
        }

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
            private set => SetProperty(ref _totalPages, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public AsyncRelayCommand ApplyFilterCommand { get; }
        public AsyncRelayCommand NextPageCommand { get; }
        public AsyncRelayCommand PreviousPageCommand { get; }
        public AsyncRelayCommand LoadInitialDataCommand { get; }

        public CallLogViewModel(
            IGetCallLogsUseCase getCallLogsUseCase,
            ILogger<CallLogViewModel> logger,
            IDialogService dialogService)
        {
            _getCallLogsUseCase = getCallLogsUseCase ?? throw new ArgumentNullException(nameof(getCallLogsUseCase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            ApplyFilterCommand = new AsyncRelayCommand(async () =>
            {
                await LoadCallLogsAsync(targetPage: 1);
            });

            NextPageCommand = new AsyncRelayCommand(
                async () => await LoadCallLogsAsync(targetPage: CurrentPage + 1),
                () => CurrentPage < TotalPages && !IsLoading);

            PreviousPageCommand = new AsyncRelayCommand(
                async () => await LoadCallLogsAsync(targetPage: CurrentPage - 1),
                () => CurrentPage > 1 && !IsLoading);

            LoadInitialDataCommand = new AsyncRelayCommand(async () =>
            {
                if (!_isInitialized)
                {
                    await LoadCallLogsAsync(targetPage: 1);
                    _isInitialized = true;
                }
            });

            _logger.LogInformation("CallLogViewModel инициализирована.");
            // ИСПРАВЛЕНИЕ АРХИТЕКТУРЫ: Убран fire-and-forget вызов _ = LoadCallLogsAsync из конструктора.
            // Теперь UI должен сам запросить данные, когда вкладка откроется (связано в MainViewModel) 
            // или юзер нажмет "Применить".
        }

        public async Task LoadCallLogsAsync(int targetPage)
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;

                _logger.LogInformation(
                    "Загрузка журнала звонков. Страница: {Page}, StartDate: {StartDate}, EndDate: {EndDate}, Phone: {Phone}",
                    targetPage,
                    FilterStartDate,
                    FilterEndDate,
                    string.IsNullOrEmpty(SearchPhoneNumber) ? "пусто" : SearchPhoneNumber);

                var result = await _getCallLogsUseCase.ExecuteAsync(
                    FilterStartDate,
                    FilterEndDate,
                    SearchPhoneNumber,
                    targetPage,
                    _pageSize);

                if (result != null)
                {
                    TotalPages = Math.Max(1, (int)Math.Ceiling((double)result.TotalCount / result.PageSize));
                    CurrentPage = targetPage;

                    Calls.Clear();
                    foreach (var record in result.Items)
                    {
                        Calls.Add(record);
                    }

                    _logger.LogInformation("Загрузка успешно завершена. Получено записей: {Count}. Всего страниц: {TotalPages}",
                        result.Items.Count(), TotalPages);

                    if (!result.Items.Any() && string.IsNullOrEmpty(SearchPhoneNumber) && FilterStartDate == null && FilterEndDate == null)
                    {
                        _logger.LogWarning("ВНИМАНИЕ: Инфраструктура вернула 0 записей! Проверь, физически ли лежит call-records.json в папке MockApiData/api/ и не пустой ли он.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла системная ошибка при загрузке журнала звонков.");
                _dialogService.ShowMessage("Ошибка", $"Произошла системная ошибка при получении данных:\n{ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}