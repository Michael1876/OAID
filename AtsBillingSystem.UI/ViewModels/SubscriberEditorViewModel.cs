using System.Threading.Tasks;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.UI.Infrastructure;
using Microsoft.EntityFrameworkCore; // Единственное исключение для перехвата EF-ошибки

namespace AtsBillingSystem.UI.ViewModels
{
    public class SubscriberEditorViewModel : ViewModelBase
    {
        private readonly IUpdateSubscriberUseCase _updateSubscriberUseCase;
        private readonly IDialogService _dialogService;

        public DomainSubscriber EditingSubscriber { get; }

        public AsyncRelayCommand SaveAsyncCommand { get; }

        public SubscriberEditorViewModel(
            DomainSubscriber subscriber,
            IUpdateSubscriberUseCase updateSubscriberUseCase,
            IDialogService dialogService)
        {
            EditingSubscriber = subscriber;
            _updateSubscriberUseCase = updateSubscriberUseCase;
            _dialogService = dialogService;
            SaveAsyncCommand = new AsyncRelayCommand(SaveAsync);
        }

        private async Task SaveAsync()
        {
            try
            {
                await _updateSubscriberUseCase.ExecuteAsync(EditingSubscriber);
                _dialogService.ShowMessage("Успех", "Данные абонента обновлены.");
            }
            catch (DbUpdateConcurrencyException)
            {
                _dialogService.ShowMessage(
                    "Конфликт версий",
                    "Данные этого абонента были изменены другим процессом (например, биллингом). " +
                    "Пожалуйста, обновите страницу и повторите попытку.");
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowMessage("Ошибка", $"Не удалось сохранить: {ex.Message}");
            }
        }
    }
}