using System;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Application.UseCases
{
    public class GetCallLogsUseCase : IGetCallLogsUseCase
    {
        private readonly ICallLogRepository _callLogRepository;

        public GetCallLogsUseCase(ICallLogRepository callLogRepository)
        {
            _callLogRepository = callLogRepository ?? throw new ArgumentNullException(nameof(callLogRepository));
        }

        public Task<PagedResult<DomainCallRecord>> ExecuteAsync(
            DateTime? start,
            DateTime? end,
            string phone,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            return _callLogRepository.GetByFilterAsync(start, end, phone ?? string.Empty, page, pageSize);
        }
    }
}
