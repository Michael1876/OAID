using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Enums;
using AtsBillingSystem.Domain.Interfaces.Repositories;
using AtsBillingSystem.Domain.Interfaces.Services;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Application.Services
{
    public class BillingService : IBillingService
    {
        private readonly ISubscriberRepository _subscriberRepository;
        private readonly ITariffRepository _tariffRepository;
        private readonly ICallLogRepository _callLogRepository;

        public BillingService(
            ISubscriberRepository subscriberRepository,
            ITariffRepository tariffRepository,
            ICallLogRepository callLogRepository)
        {
            _subscriberRepository = subscriberRepository;
            _tariffRepository = tariffRepository;
            _callLogRepository = callLogRepository;
        }

        public decimal CalculateCost(int durationSeconds, CallType type, DomainTariff tariff)
        {
            // Бизнес-правило: Первые 3 секунды не тарифицируются
            if (durationSeconds <= 3) return 0m;

            decimal pricePerMinute = type == CallType.Internal
                ? tariff.InternalMinutePrice
                : tariff.CityMinutePrice;

            // Стоимость = (длительность / 60) * цена_за_минуту + плата_за_соединение
            // Переводим секунды в минуты (долевое значение, а не целочисленное деление!)
            decimal durationMinutes = durationSeconds / 60m;
            decimal rawCost = (durationMinutes * pricePerMinute) + tariff.ConnectionFee;

            // Бизнес-правило: Округление до копеек (2 знака)
            return Math.Round(rawCost, 2, MidpointRounding.AwayFromZero);
        }

        public async Task<BillingResult> ProcessCdrBatchAsync(IEnumerable<ParsedCallDto> parsedCalls, Action<int> progressCallback)
        {
            var result = new BillingResult { IsSuccess = true };
            var callsList = parsedCalls.ToList();

            // 1. Извлекаем ВСЕ уникальные номера инициаторов звонков
            var uniqueCallerPhones = callsList.Select(c => c.CallerPhone).Distinct().ToList();

            // 2. ЗАЩИТА ОТ N+1: Получаем всех нужных абонентов за ОДИН запрос к БД
            var subscribersDict = await _subscriberRepository.GetByPhonesBatchAsync(uniqueCallerPhones);

            // 3. Кешируем активные тарифы в памяти
            var activeTariffs = (await _tariffRepository.GetActiveAsync()).ToDictionary(t => t.Id);

            var newCallRecords = new List<DomainCallRecord>();
            var subscribersToUpdate = new HashSet<DomainSubscriber>(); // HashSet исключает дубликаты обновлений

            int processedCount = 0;
            int totalCount = callsList.Count;

            // 4. Обсчет в оперативной памяти (без обращений к БД)
            foreach (var call in callsList)
            {
                if (!subscribersDict.TryGetValue(call.CallerPhone, out var subscriber))
                {
                    result.FailedItems.Add($"Абонент {call.CallerPhone} не найден.");
                    continue;
                }

                if (!activeTariffs.TryGetValue(subscriber.TariffId, out var tariff))
                {
                    result.FailedItems.Add($"Тариф абонента {call.CallerPhone} не найден или в архиве.");
                    continue;
                }

                // Определение типа звонка (упрощенная логика: если префикс одинаковый - внутрисетевой)
                CallType callType = (call.CallerPhone.Substring(0, 4) == call.ReceiverPhone.Substring(0, 4))
                    ? CallType.Internal : CallType.City;

                decimal cost = CalculateCost(call.DurationSeconds, callType, tariff);

                // Списываем деньги через доменный метод
                subscriber.DebitBalance(cost);
                subscribersToUpdate.Add(subscriber);

                newCallRecords.Add(new DomainCallRecord
                {
                    Id = Guid.NewGuid(),
                    SubscriberId = subscriber.Id,
                    DestinationNumber = call.ReceiverPhone,
                    StartTime = call.StartTime,
                    DurationSeconds = call.DurationSeconds,
                    CallType = callType,
                    Cost = cost
                });

                processedCount++;
                if (processedCount % 100 == 0) // Обновляем UI каждые 100 записей
                {
                    int percent = (int)((double)processedCount / totalCount * 100);
                    progressCallback?.Invoke(percent);
                }
            }

            // 5. Передаем обработанные данные в репозитории (БЕЗ SaveChanges)
            await _callLogRepository.AddRangeAsync(newCallRecords);
            await _subscriberRepository.UpdateBalancesBatchAsync(subscribersToUpdate);

            progressCallback?.Invoke(100); // Завершено
            return result;
        }
    }
}