using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AtsBillingSystem.Infrastructure.Json.Api;

namespace AtsBillingSystem.Infrastructure.Json.Storage;

public interface IFileStorage
{
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task SaveAsync(string relativePath, Func<Stream, Task> writeAction, CancellationToken cancellationToken = default);
}

public class LocalFileStorage : IFileStorage
{
    private readonly JsonApiOptions _options;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(JsonApiOptions options, ILogger<LocalFileStorage> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = MapToFilePath(relativePath);
        if (!File.Exists(path))
        {
            _logger.LogWarning("Файл не найден по пути: {Path}", path);
            return Task.FromResult<Stream?>(null);
        }

        try
        {
            // Открываем так, чтобы не блокировать чтение другими процессами WPF (FileShare.ReadWrite)
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Task.FromResult<Stream?>(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка открытия файла для чтения: {Path}", path);
            return Task.FromResult<Stream?>(null);
        }
    }

    public async Task SaveAsync(string relativePath, Func<Stream, Task> writeAction, CancellationToken cancellationToken = default)
    {
        var path = MapToFilePath(relativePath);
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await writeAction(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения файла: {Path}", path);
            throw;
        }
    }

    private string MapToFilePath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        return Path.Combine(ResolveDataDirectory(), normalized);
    }

    private string ResolveDataDirectory()
    {
        if (Path.IsPathRooted(_options.DataDirectory)) return _options.DataDirectory;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, _options.DataDirectory);
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return Path.Combine(AppContext.BaseDirectory, _options.DataDirectory);
    }
}