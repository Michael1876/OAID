using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AtsBillingSystem.Application.Services;

namespace AtsBillingSystem.Tests.Application.Services
{
    [TestFixture]
    public class CdrCsvParserTests
    {
        private CdrCsvParser _parser;
        private string _tempFilePath;

        [SetUp]
        public void Setup()
        {
            _parser = new CdrCsvParser();
            // создаем реальный временный файл на диске,
            // так как CdrCsvParser жестко зависит от FileStream.
            _tempFilePath = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            // Обязательно убираем за собой, чтобы не засорять диск
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        [Test]
        public async Task ParseAsync_ValidCsvFormat_ParsesSuccessfully()
        {
            // Arrange
            var csvContent =
                "2025-05-29 10:15:00;74951234501;74951234599;180\n" +
                "2025-05-29 10:22:00;74951234502;74957654321;240";
            await File.WriteAllTextAsync(_tempFilePath, csvContent);

            // Act
            var result = await _parser.ParseAsync(_tempFilePath);
            var resultList = result.ToList();

            // Assert
            Assert.That(resultList, Has.Count.EqualTo(2));

            Assert.That(resultList[0].CallerPhone, Is.EqualTo("74951234501"));
            Assert.That(resultList[0].ReceiverPhone, Is.EqualTo("74951234599"));
            Assert.That(resultList[0].DurationSeconds, Is.EqualTo(180));
            Assert.That(resultList[0].StartTime, Is.EqualTo(new DateTime(2025, 5, 29, 10, 15, 0)));
        }

        [Test]
        public async Task ParseAsync_ContainsInvalidLines_SkipsInvalidAndParsesValid()
        {
            // Arrange
            var csvContent =
                "2025-05-29 10:15:00;74951234501;74951234599;180\n" + // Валидная
                "\n" +                                                 // Пустая строка (пропуск)
                "Неправильный формат;Совсем;Без;Смысла\n" +            // Битые данные (пропуск)
                "2025-05-29 10:22:00;74951234502;74957654321;240\n" +  // Валидная
                "2025-05-29 10:22:00;74951234502;74957654321";         // Не хватает колонок (пропуск)

            await File.WriteAllTextAsync(_tempFilePath, csvContent);

            // Act
            var result = await _parser.ParseAsync(_tempFilePath);
            var resultList = result.ToList();

            // Assert
            Assert.That(resultList, Has.Count.EqualTo(2), "Парсер должен был проигнорировать пустые и битые строки");
            Assert.That(resultList[1].DurationSeconds, Is.EqualTo(240));
        }

        [Test]
        public void ValidateFileHash_FileExists_ReturnsTrue()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, "Some content for hashing");

            // Act
            var isValid = _parser.ValidateFileHash(_tempFilePath);

            // Assert
            // В текущей реализации метод возвращает true, если хэш удалось вычислить
            Assert.That(isValid, Is.True);
        }
    }
}