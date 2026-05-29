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
                return new BillingResult { IsSuccess = false, ErrorMessage = "Файл поврежден или уже был обработан ранее." };
            }

            try
            {
                // 1. Парсинг файла (только чтение)
                var parsedCalls = await _fileParser.ParseAsync(filePath);

                // 2. Открываем транзакцию БД
                await _unitOfWork.BeginTransactionAsync();

                // 3. Выполняем обсчет и передаем сущности в EF Контекст
                var billingResult = await _billingService.ProcessCdrBatchAsync(parsedCalls, progressCallback);

                if (!billingResult.IsSuccess && billingResult.FailedItems.Count > 0)
                {
                    // Логика: если есть критичные ошибки, можем откатить
                    // Но допустим, мы пропускаем битые строки и сохраняем успешные
                }

                // 4. Физическое сохранение в БД
                await _unitOfWork.SaveChangesAsync();

                // 5. Подтверждаем транзакцию
                await _unitOfWork.CommitTransactionAsync();

                return billingResult;
            }
            catch (Exception ex)
            {
                // При любой ошибке (например DbUpdateConcurrencyException) транзакция откатывается
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