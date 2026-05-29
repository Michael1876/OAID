using System.Threading.Tasks;

namespace AtsBillingSystem.Domain.Interfaces.Infrastructure
{
    public interface IUnitOfWork
    {
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        // Метод для сохранения всех измененных сущностей в контексте
        Task SaveChangesAsync();
    }

    // Для развязки MVVM
    public interface IDialogService
    {
        void ShowMessage(string title, string message);
        string OpenFileDialog(string filter);
    }

    public interface INavigationService
    {
        void NavigateTo(string viewName);
    }
}