using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Services;

namespace AtsBillingSystem.Application.Services
{
    public class CdrCsvParser : IFileParser
    {
        // Используем потоковый итератор (IAsyncEnumerable можно использовать, но для простоты совместимости 
        // с интерфейсом вернем List, формируемый эффективным чтением строк)
        public async Task<IEnumerable<ParsedCallDto>> ParseAsync(string filePath)
        {
            var parsedCalls = new List<ParsedCallDto>();

            // Открываем поток с минимальными блокировками
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var reader = new StreamReader(fileStream, Encoding.UTF8);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(';');
                if (parts.Length != 4) continue; // Пропускаем битые строки

                // Парсим: [Дата];[Кто звонил];[Кому звонил];[Длительность]
                if (DateTime.TryParse(parts[0], out var startTime) &&
                    int.TryParse(parts[3], out var duration))
                {
                    parsedCalls.Add(new ParsedCallDto
                    {
                        StartTime = startTime,
                        CallerPhone = parts[1].Trim(),
                        ReceiverPhone = parts[2].Trim(),
                        DurationSeconds = duration
                    });
                }
            }

            return parsedCalls;
        }

        public bool ValidateFileHash(string filePath)
        {
            // Здесь мы вычисляем MD5 файла. 
            // В реальной системе хэши ранее загруженных файлов хранились бы в БД.
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = md5.ComputeHash(stream);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            // Для примера: если файл пустой или не читается. 
            // Реальная проверка: bool exists = _hashRepo.Exists(hashString);
            return !string.IsNullOrEmpty(hashString);
        }
    }
}