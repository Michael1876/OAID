using System.Windows;
using Microsoft.Win32; // Для OpenFileDialog
using AtsBillingSystem.Domain.Interfaces.Infrastructure;

namespace AtsBillingSystem.UI.Infrastructure
{
    public class WpfDialogService : IDialogService
    {
        public void ShowMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public string OpenFileDialog(string filter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                Title = "Выберите файл"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return string.Empty;
        }
    }
}