using System;
using System.Windows.Input;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.UI.Infrastructure;

namespace AtsBillingSystem.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;
        private readonly Func<DomainSubscriber?, SubscriberEditorViewModel> _editorFactory;
        private readonly SubscribersViewModel _subscribersViewModel;

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ICommand NavigateToBillingCommand { get; }
        public ICommand NavigateToSubscribersCommand { get; }
        public ICommand NavigateToCallLogCommand { get; }
        public ICommand NavigateToTariffsCommand { get; }

        public MainViewModel(
            BillingProcessingViewModel billingViewModel,
            SubscribersViewModel subscribersViewModel,
            CallLogViewModel callLogViewModel,
            TariffsViewModel tariffsViewModel,
            Func<DomainSubscriber?, SubscriberEditorViewModel> editorFactory)
        {
            _subscribersViewModel = subscribersViewModel;
            _editorFactory = editorFactory;
            _currentViewModel = subscribersViewModel;

            _subscribersViewModel.NavigateToEditorRequested += OnNavigateToEditor;

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
                callLogViewModel.LoadInitialDataCommand.Execute(null);
                return System.Threading.Tasks.Task.CompletedTask;
            });

            NavigateToTariffsCommand = new AsyncRelayCommand(() =>
            {
                CurrentViewModel = tariffsViewModel;
                return System.Threading.Tasks.Task.CompletedTask;
            });
        }

        private void OnNavigateToEditor(DomainSubscriber? subscriber)
        {
            var editorVm = _editorFactory(subscriber);

            editorVm.WorkCompleted += () =>
            {
                CurrentViewModel = _subscribersViewModel;
                if (_subscribersViewModel.LoadPageCommand.CanExecute(null))
                {
                    _subscribersViewModel.LoadPageCommand.Execute(null);
                }
            };

            CurrentViewModel = editorVm;
        }
    }
}