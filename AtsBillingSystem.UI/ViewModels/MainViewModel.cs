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

        // Команды навигации
        public ICommand NavigateToBillingCommand { get; }
        public ICommand NavigateToSubscribersCommand { get; }
        public ICommand NavigateToCallLogCommand { get; }
        public ICommand NavigateToTariffsCommand { get; }

        // DI-контейнер (в App.xaml.cs) сам инжектит все необходимые ViewModel
        public MainViewModel(
            BillingProcessingViewModel billingViewModel,
            SubscribersViewModel subscribersViewModel,
            CallLogViewModel callLogViewModel,
            TariffsViewModel tariffsViewModel)
        {
            // Устанавливаем стартовый экран
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
                // Загружаем данные только в момент перехода на вкладку (ленивая загрузка)
                callLogViewModel.LoadInitialDataCommand.Execute(null);

                return System.Threading.Tasks.Task.CompletedTask;
            });

            NavigateToTariffsCommand = new AsyncRelayCommand(() =>
            {
                CurrentViewModel = tariffsViewModel;
                return System.Threading.Tasks.Task.CompletedTask;
            });
        }
    }
}