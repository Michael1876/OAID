using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.UI.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AtsBillingSystem.UI.ViewModels
{
    public class SubscriberEditorViewModel : ViewModelBase
    {
        private readonly IUpdateSubscriberUseCase _updateSubscriberUseCase;
        private readonly IAddSubscriberUseCase _addSubscriberUseCase;
        private readonly IGetActiveTariffsUseCase _getActiveTariffsUseCase;
        private readonly IDialogService _dialogService;
        private readonly bool _isNewMode;

        public DomainSubscriber EditingSubscriber { get; }
        public ObservableCollection<DomainTariff> Tariffs { get; } = new();

        // Событие для MainViewModel, чтобы вернуться на предыдущий экран
        public event Action? WorkCompleted;

        public AsyncRelayCommand SaveAsyncCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }

        public SubscriberEditorViewModel(
            DomainSubscriber? subscriber,
            IAddSubscriberUseCase addSubscriberUseCase,
            IUpdateSubscriberUseCase updateSubscriberUseCase,
            IGetActiveTariffsUseCase getActiveTariffsUseCase,
            IDialogService dialogService)
        {
            _addSubscriberUseCase = addSubscriberUseCase;
            _updateSubscriberUseCase = updateSubscriberUseCase;
            _getActiveTariffsUseCase = getActiveTariffsUseCase;
            _dialogService = dialogService;

            // Если передан null — значит, мы создаем нового абонента
            _isNewMode = subscriber == null;
            EditingSubscriber = subscriber ?? new DomainSubscriber { IsActive = true };

            SaveAsyncCommand = new AsyncRelayCommand(SaveAsync);
            CancelCommand = new AsyncRelayCommand(() => { WorkCompleted?.Invoke(); return Task.CompletedTask; });

            _ = LoadTariffsAsync();
        }

        private async Task LoadTariffsAsync()
        {
            try
            {
                Tariffs.Clear();
                var activeTariffs = await _getActiveTariffsUseCase.ExecuteAsync();
                foreach (var tariff in activeTariffs)
                {
                    Tariffs.Add(tariff);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage("Ошибка", $"Не удалось загрузить тарифы: {ex.Message}");
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                if (_isNewMode)
                {
                    await _addSubscriberUseCase.ExecuteAsync(EditingSubscriber);
                    _dialogService.ShowMessage("Успех", "Новый абонент успешно зарегистрирован.");
                }
                else
                {
                    await _updateSubscriberUseCase.ExecuteAsync(EditingSubscriber);
                    _dialogService.ShowMessage("Успех", "Данные абонента обновлены.");
                }

                WorkCompleted?.Invoke(); // Возвращаемся к списку
            }
            catch (DbUpdateConcurrencyException)
            {
                _dialogService.ShowMessage(
                    "Конфликт версий",
                    "Данные этого абонента были изменены другим процессом. Пожалуйста, обновите страницу.");
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage("Ошибка", $"Не удалось сохранить: {ex.Message}");
            }
        }
    }
}