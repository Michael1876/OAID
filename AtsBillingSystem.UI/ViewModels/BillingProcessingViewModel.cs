using System;
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
            _processBillingUseCase = processBillingUseCase;
            _dialogService = dialogService;
            SelectFileAndExecuteAsyncCommand = new AsyncRelayCommand(ExecuteImportAsync, () => !IsProcessing);
        }

        private async Task ExecuteImportAsync()
        {
            var filePath = _dialogService.OpenFileDialog("CSV Files (*.csv)|*.csv");
            if (string.IsNullOrEmpty(filePath)) return;

            IsProcessing = true;
            ProgressBarValue = 0;
            StatusMessage = "Обработка файла...";

            // Action, который мы передадим в глубины слоя Application
            Action<int> updateProgress = percent =>
            {
                // В WPF/Avalonia привязка данных (Binding) сама разруливает потоки для простых свойств,
                // но если бы мы меняли коллекции (ObservableCollection), тут потребовался бы Dispatcher
                ProgressBarValue = percent;
            };

            var result = await _processBillingUseCase.ExecuteAsync(filePath, updateProgress);

            IsProcessing = false;

            if (result.IsSuccess)
            {
                StatusMessage = "Импорт успешно завершен.";
                _dialogService.ShowMessage("Успех", "Тарификация успешно применена, данные сохранены.");
            }
            else
            {
                StatusMessage = "Ошибка импорта.";
                _dialogService.ShowMessage("Ошибка", result.ErrorMessage);
            }
        }
    }
}