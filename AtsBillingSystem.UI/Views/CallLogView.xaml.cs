using System.Windows;
using System.Windows.Controls;
using AtsBillingSystem.UI.ViewModels;

namespace AtsBillingSystem.UI.Views
{
    public partial class CallLogView : UserControl
    {
        public CallLogView()
        {
            InitializeComponent();

            // Архитектурно правильный способ связать View и ViewModel без нарушения MVVM:
            // Как только интерфейс отрисовался, мы просим ViewModel загрузить первичные данные.
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CallLogViewModel viewModel)
            {
                // Запускаем безопасную инициализацию
                viewModel.LoadInitialDataCommand.Execute(null);
            }
        }
    }
}