using System.Windows.Input;
using AtsBillingSystem.UI.Infrastructure;

namespace AtsBillingSystem.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ICommand NavigateToBillingCommand { get; }
        public ICommand NavigateToSubscribersCommand { get; }
        public ICommand NavigateToCallLogCommand { get; }

        public MainViewModel(
            BillingProcessingViewModel billingViewModel,
            SubscribersViewModel subscribersViewModel,
            CallLogViewModel callLogViewModel)
        {
            _currentViewModel = subscribersViewModel;

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
                // ИСПРАВЛЕНИЕ: Вызываем загрузку данных ТОЛЬКО когда пользователь реально зашел на вкладку.
                // Используем Execute(null), так как интерфейс ICommand не имеет ExecuteAsync.
                // Команда внутри сама выполнит Task асинхронно без блокировки потока UI.
                callLogViewModel.LoadInitialDataCommand.Execute(null);

                return System.Threading.Tasks.Task.CompletedTask;
            });
        }
    }
}