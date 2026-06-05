using System;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.Domain.Interfaces.Services;
using AtsBillingSystem.Domain.Interfaces.UseCases;

namespace AtsBillingSystem.Application.UseCases
{
    public class ProcessBillingUseCase : IProcessBillingUseCase
    {
        private readonly IFileParser _fileParser;
        private readonly IBillingService _billingService;
        private readonly IUnitOfWork _unitOfWork;

        public ProcessBillingUseCase(
            IFileParser fileParser,
            IBillingService billingService,
            IUnitOfWork unitOfWork)
        {
            _fileParser = fileParser;
            _billingService = billingService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BillingResult> ExecuteAsync(string filePath, Action<int> progressCallback)
        {
            if (!_fileParser.ValidateFileHash(filePath))
            {
                return new BillingResult { IsSuccess = false, ErrorMessage = "Защита от дублирования: этот файл уже был успешно тарифицирован ранее." };
            }

            try
            {
                var parsedCalls = await _fileParser.ParseAsync(filePath);

                await _unitOfWork.BeginTransactionAsync();

                var billingResult = await _billingService.ProcessCdrBatchAsync(parsedCalls, progressCallback);

                await _unitOfWork.SaveChangesAsync();

                // Фиксируем хэш только если всё сохранилось без проблем
                _fileParser.MarkFileAsProcessed(filePath);

                await _unitOfWork.CommitTransactionAsync();

                return billingResult;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                return new BillingResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка при обработке биллинга: {ex.Message}"
                };
            }
        }
    }
}