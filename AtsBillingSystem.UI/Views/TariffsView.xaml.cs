using System.Windows;
using System.Windows.Controls;
using AtsBillingSystem.UI.ViewModels;

namespace AtsBillingSystem.UI.Views
{
    public partial class TariffsView : UserControl
    {
        public TariffsView()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                if (DataContext is TariffsViewModel vm) vm.LoadTariffsCommand.Execute(null);
            };
        }
    }
}