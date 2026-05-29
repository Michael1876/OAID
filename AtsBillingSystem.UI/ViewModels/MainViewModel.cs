using System.Windows.Input;
using AtsBillingSystem.UI.Infrastructure;

namespace AtsBillingSystem.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;

        // Текущий активный экран
        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        // Команды для кнопок меню
        public ICommand NavigateToBillingCommand { get; }
        public ICommand NavigateToSubscribersCommand { get; }
        public ICommand NavigateToCallLogCommand { get; }

        public MainViewModel(
            BillingProcessingViewModel billingViewModel,
            SubscribersViewModel subscribersViewModel,
            CallLogViewModel callLogViewModel)
        {
            // По умолчанию при старте открываем экран абонентов
            _currentViewModel = subscribersViewModel;

            // В WPF можно использовать синхронные команды для простой смены ссылок в памяти.
            // Здесь я использую твой AsyncRelayCommand, просто возвращая Task.CompletedTask
            NavigateToBillingCommand = new AsyncRelayCommand(() =>
            {
                CurrentViewModel = billingViewModel;
                return System.Threading.Tasks.Task.CompletedTask;
            });

            NavigateToSubscribersCommand = new AsyncRelayCommand(() =>
            {
                CurrentViewModel = subscribersViewModel;
                return System.Threading.Tasks.Task.CompletedTask;
            });

            NavigateToCallLogCommand = new AsyncRelayCommand(() =>
            {
                CurrentViewModel = callLogViewModel;
                return System.Threading.Tasks.Task.CompletedTask;
            });
        }
    }
}