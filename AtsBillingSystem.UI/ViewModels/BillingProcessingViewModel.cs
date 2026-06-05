using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.UI.Infrastructure;

namespace AtsBillingSystem.UI.ViewModels
{
    public class BillingProcessingViewModel : ViewModelBase
    {
        private readonly IProcessBillingUseCase _processBillingUseCase;
        private readonly IDialogService _dialogService;

        public ObservableCollection<string> FailedItems { get; } = new();

        private bool _hasErrors;
        public bool HasErrors
        {
            get => _hasErrors;
            set => SetProperty(ref _hasErrors, value);
        }

        private int _progressBarValue;
        public int ProgressBarValue
        {
            get => _progressBarValue;
            set => SetProperty(ref _progressBarValue, value);
        }

        private string _statusMessage = "Ожидание выбора файла...";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public AsyncRelayCommand SelectFileAndExecuteAsyncCommand { get; }

        public BillingProcessingViewModel(IProcessBillingUseCase processBillingUseCase, IDialogService dialogService)
        {
            _processBillingUseCase = processBillingUseCase ?? throw new ArgumentNullException(nameof(processBillingUseCase));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Кнопка кликабельна только тогда, когда не запущен фоновый процесс
            SelectFileAndExecuteAsyncCommand = new AsyncRelayCommand(ExecuteImportAsync, () => !IsProcessing);
        }

        private async Task ExecuteImportAsync()
        {
            string filePath = string.Empty;

            try
            {
                filePath = _dialogService.OpenFileDialog("CSV Files (*.csv)|*.csv");
                if (string.IsNullOrEmpty(filePath)) return;

                IsProcessing = true;
                HasErrors = false;
                FailedItems.Clear();
                ProgressBarValue = 0;
                StatusMessage = "Обработка файла...";

                Action<int> updateProgress = percent =>
                {
                    ProgressBarValue = percent;
                };

                var result = await _processBillingUseCase.ExecuteAsync(filePath, updateProgress);

                if (result.FailedItems != null && result.FailedItems.Count > 0)
                {
                    HasErrors = true;
                    foreach (var item in result.FailedItems)
                    {
                        FailedItems.Add(item);
                    }
                }

                if (result.IsSuccess)
                {
                    StatusMessage = HasErrors ? "Импорт завершен, но часть строк пропущена (см. ниже)." : "Импорт успешно завершен.";
                    _dialogService.ShowMessage("Успех", "Тарификация успешно применена, данные сохранены.");
                }
                else
                {
                    StatusMessage = "Ошибка импорта.";
                    _dialogService.ShowMessage("Ошибка", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Критический сбой при обработке.";
                _dialogService.ShowMessage("Критическая ошибка", $"При выполнении операции произошло системное исключение:\n{ex.Message}");
            }
            finally
            {
                // Кнопка гарантированно разблокируется при любом исходе
                IsProcessing = false;
            }
        }
    }
}