using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AtsBillingSystem.Domain.Common;
using AtsBillingSystem.Domain.Interfaces.Services;

namespace AtsBillingSystem.Application.Services
{
    public class CdrCsvParser : IFileParser
    {
        // Храним хэши в простом текстовом файле локально
        private readonly string _hashStoragePath = Path.Combine(AppContext.BaseDirectory, "processed_hashes.txt");

        public async Task<IEnumerable<ParsedCallDto>> ParseAsync(string filePath)
        {
            var parsedCalls = new List<ParsedCallDto>();

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var reader = new StreamReader(fileStream, Encoding.UTF8);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(';');
                if (parts.Length != 4) continue; // Пропускаем битые строки

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
            var hashString = CalculateHash(filePath);

            if (!File.Exists(_hashStoragePath)) return true; // Кэша еще нет, файл новый

            var processedHashes = File.ReadAllLines(_hashStoragePath);
            return !processedHashes.Contains(hashString);
        }

        public void MarkFileAsProcessed(string filePath)
        {
            var hashString = CalculateHash(filePath);
            File.AppendAllText(_hashStoragePath, hashString + Environment.NewLine);
        }

        private string CalculateHash(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = md5.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}