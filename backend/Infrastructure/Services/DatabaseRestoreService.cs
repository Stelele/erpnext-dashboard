using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseRestoreService : IHostedService
{
    public static void EnsureDatabaseExists(IR2StorageService r2, IConfiguration configuration, ILogger<DatabaseRestoreService> logger)
    {
        var connectionString = configuration.GetConnectionString("Sqlite") 
            ?? throw new InvalidOperationException("Connection string 'Sqlite' not found.");
        var databasePath = connectionString.Replace("Data Source=", "");

        if (File.Exists(databasePath))
        {
            logger.LogInformation("Database already exists at {Path}, skipping restore", databasePath);
            return;
        }

        logger.LogInformation("No database found at {Path}, attempting restore from R2", databasePath);

        var fileName = Path.GetFileName(databasePath);
        var backupKey = $"erpnext-dashboard/{r2.Environment}/database/{fileName}";
        var maxRetries = configuration.GetValue<int?>("R2:RestoreMaxRetries") ?? 3;
        var cancellationToken = CancellationToken.None;

        try
        {
            var task = RestoreDatabaseAsyncInternal(r2, backupKey, maxRetries, databasePath, logger, cancellationToken);
            task.Wait(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database restore failed, will continue with new database");
        }
    }

    private static async Task RestoreDatabaseAsyncInternal(IR2StorageService r2, string backupKey, int maxRetries, string databasePath, ILogger<DatabaseRestoreService> logger, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(databasePath);
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting to restore database (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                if (!await r2.ObjectExistsAsync(r2.BackupBucket, backupKey, cancellationToken))
                {
                    logger.LogWarning("No backup found in R2: {FileName}, creating new database", fileName);
                    return;
                }

                var stream = await r2.DownloadAsync(r2.BackupBucket, backupKey, cancellationToken);
                await using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, cancellationToken);

                var directory = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(databasePath, memoryStream.ToArray(), cancellationToken);

                logger.LogInformation("Database restored successfully from R2");
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

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
