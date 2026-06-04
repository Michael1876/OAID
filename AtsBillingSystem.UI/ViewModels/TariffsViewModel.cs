using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Models;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.UI.Infrastructure;

namespace AtsBillingSystem.UI.ViewModels
{
    public class TariffsViewModel : ViewModelBase
    {
        private readonly IGetActiveTariffsUseCase _getActiveTariffsUseCase;

        public ObservableCollection<DomainTariff> Tariffs { get; } = new();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public AsyncRelayCommand LoadTariffsCommand { get; }

        public TariffsViewModel(IGetActiveTariffsUseCase getActiveTariffsUseCase)
        {
            _getActiveTariffsUseCase = getActiveTariffsUseCase;
            LoadTariffsCommand = new AsyncRelayCommand(LoadTariffsAsync);
        }

        private async Task LoadTariffsAsync()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                Tariffs.Clear();
                var tariffs = await _getActiveTariffsUseCase.ExecuteAsync();
                foreach (var tariff in tariffs) Tariffs.Add(tariff);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}