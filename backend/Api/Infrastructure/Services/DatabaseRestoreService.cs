using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseRestoreService(
    ILogger<DatabaseRestoreService> logger,
    GoogleDriveService googleDrive,
    IConfiguration configuration) : IHostedService
{
    private readonly ILogger<DatabaseRestoreService> _logger = logger;
    private readonly GoogleDriveService _googleDrive = googleDrive;
    private readonly int _maxRetries = configuration.GetValue<int?>("GoogleDrive:RestoreMaxRetries") ?? 3;

    public static void EnsureDatabaseExists(GoogleDriveService googleDrive, IConfiguration configuration, ILogger<DatabaseRestoreService> logger)
    {
        if (File.Exists(googleDrive.DatabasePath))
        {
            logger.LogInformation("Database already exists at {Path}, skipping restore", googleDrive.DatabasePath);
            return;
        }

        logger.LogInformation("No database found at {Path}, attempting restore from Google Drive", googleDrive.DatabasePath);

        var fileName = Path.GetFileName(googleDrive.DatabasePath);
        var maxRetries = configuration.GetValue<int?>("GoogleDrive:RestoreMaxRetries") ?? 3;
        var cancellationToken = CancellationToken.None;

        try
        {
            RestoreDatabaseAsyncInternal(googleDrive, fileName, maxRetries, logger, cancellationToken).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database restore failed, will continue with new database");
        }
    }

    private static async Task RestoreDatabaseAsyncInternal(GoogleDriveService googleDrive, string fileName, int maxRetries, ILogger<DatabaseRestoreService> logger, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting to restore database (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                var existingFile = await googleDrive.FindFileAsync(fileName, cancellationToken);

                if (existingFile is null)
                {
                    logger.LogWarning("No backup found in Google Drive: {FileName}, creating new database", fileName);
                    return;
                }

                using var memoryStream = new MemoryStream();
                await googleDrive.DownloadFileAsync(existingFile.Id, memoryStream, cancellationToken);

                var directory = Path.GetDirectoryName(googleDrive.DatabasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var dbPath = googleDrive.DatabasePath;
                var directory2 = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(directory2) && !Directory.Exists(directory2))
                {
                    Directory.CreateDirectory(directory2);
                }

                await File.WriteAllBytesAsync(dbPath, memoryStream.ToArray(), cancellationToken);

                logger.LogInformation("Database restored successfully from Google Drive");
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                logger.LogWarning(ex, "Database restore attempt {Attempt} failed, retrying in {Delay}s", attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database restore failed after {MaxRetries} attempts", maxRetries);
                throw;
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}