namespace SafeHorizons.Api.Services;

public interface IFileCleanupService
{
    Task CleanupOldFilesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
    Task<int> GetOldFilesCountAsync(TimeSpan maxAge);
}

public class FileCleanupService : IFileCleanupService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileCleanupService> _logger;

    public FileCleanupService(IWebHostEnvironment environment, ILogger<FileCleanupService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task CleanupOldFilesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        try
        {
            var wwwrootPath = _environment.WebRootPath;
            if (!Directory.Exists(wwwrootPath))
                return;

            var cutoffTime = DateTime.UtcNow - maxAge;
            var files = Directory.GetFiles(wwwrootPath);
            var deletedCount = 0;

            foreach (var filePath in files)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.LastWriteTimeUtc < cutoffTime && fileInfo.Extension == ".drawio")
                {
                    try
                    {
                        File.Delete(filePath);
                        deletedCount++;
                        _logger.LogInformation("Deleted old file: {FileName}", Path.GetFileName(filePath));
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file cleanup");
        }
    }

    public Task<int> GetOldFilesCountAsync(TimeSpan maxAge)
    {
        var wwwrootPath = _environment.WebRootPath;
        if (!Directory.Exists(wwwrootPath))
            return Task.FromResult(0);

        var cutoffTime = DateTime.UtcNow - maxAge;
        var oldFiles = Directory.GetFiles(wwwrootPath)
            .Where(f => new FileInfo(f).LastWriteTimeUtc < cutoffTime &&
                       Path.GetExtension(f) == ".drawio")
            .Count();

        return Task.FromResult(oldFiles);
    }
}