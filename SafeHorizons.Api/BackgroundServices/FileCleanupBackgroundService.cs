namespace SafeHorizons.Api.BackgroundServices;

public class FileCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileCleanupBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public FileCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FileCleanupBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Читаем настройки из конфигурации
        var cleanupIntervalMinutes = _configuration.GetValue<int>("FileCleanup:CleanupIntervalMinutes", 15);
        var fileMaxAgeMinutes = _configuration.GetValue<int>("FileCleanup:FileMaxAgeMinutes", 30);

        _logger.LogInformation(
            "File Cleanup Service started. Interval: {Interval}min, MaxAge: {MaxAge}min",
            cleanupIntervalMinutes, fileMaxAgeMinutes);

        // Ждем немного перед первым запуском
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Starting cleanup cycle...");

                using var scope = _serviceProvider.CreateScope();
                var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

                // ✅ Добавляем await!
                await CleanupFilesAsync(environment, TimeSpan.FromMinutes(fileMaxAgeMinutes), stoppingToken);

                _logger.LogDebug("Cleanup cycle completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in File Cleanup Service");
            }

            // Ждем следующий запуск
            _logger.LogDebug("Waiting {Minutes} minutes until next cleanup", cleanupIntervalMinutes);
            await Task.Delay(TimeSpan.FromMinutes(cleanupIntervalMinutes), stoppingToken);
        }
    }

    // ✅ Меняем на async метод
    private async Task CleanupFilesAsync(IWebHostEnvironment environment, TimeSpan maxAge, CancellationToken cancellationToken)
    {
        var wwwrootPath = environment.WebRootPath;
        if (!Directory.Exists(wwwrootPath))
        {
            _logger.LogWarning("wwwroot directory does not exist: {Path}", wwwrootPath);
            return;
        }

        var cutoffTime = DateTime.UtcNow - maxAge;
        var files = Directory.GetFiles(wwwrootPath);
        var deletedCount = 0;

        _logger.LogInformation("Checking {FileCount} files in {Path}", files.Length, wwwrootPath);

        foreach (var filePath in files)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cleanup cancelled");
                break;
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.LastWriteTimeUtc < cutoffTime && fileInfo.Extension == ".drawio")
            {
                try
                {
                    // ✅ Добавляем небольшую задержку для асинхронности
                    await Task.Delay(10, cancellationToken);

                    File.Delete(filePath);
                    deletedCount++;
                    _logger.LogInformation("Deleted old file: {FileName} (Created: {Created})",
                        Path.GetFileName(filePath), fileInfo.LastWriteTimeUtc);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file: {FileName}", Path.GetFileName(filePath));
                }
            }
        }

        if (deletedCount > 0)
        {
            _logger.LogInformation("Deleted {Count} old files older than {Minutes} minutes",
                deletedCount, maxAge.TotalMinutes);
        }
        else
        {
            _logger.LogDebug("No old files found for cleanup");
        }
    }
}