using System.Collections.Generic;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Application.UseCases
{
    public class GetActiveTariffsUseCase : IGetActiveTariffsUseCase
    {
        private readonly ITariffRepository _tariffRepository;

        public GetActiveTariffsUseCase(ITariffRepository tariffRepository)
        {
            _tariffRepository = tariffRepository;
        }

        public async Task<IEnumerable<DomainTariff>> ExecuteAsync()
        {
            // Получаем только активные тарифы (без архивных) через репозиторий
            return await _tariffRepository.GetActiveAsync();
        }
    }
}