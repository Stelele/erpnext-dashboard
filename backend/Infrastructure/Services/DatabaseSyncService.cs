using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseSyncService(
    ILogger<DatabaseSyncService> logger,
    IR2StorageService r2,
    IConfiguration configuration
) : BackgroundService, IDatabaseSyncService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(
        configuration.GetValue<int?>("R2:SyncIntervalMinutes") ?? 15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Database sync service started. Sync interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await SyncDatabaseAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Database sync service stopping");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during database sync");
            }
        }
    }

    public async Task SyncDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("Sqlite") 
            ?? throw new InvalidOperationException("Connection string 'Sqlite' not found.");
        var databasePath = connectionString.Replace("Data Source=", "");

        if (!File.Exists(databasePath))
        {
            logger.LogWarning("Database file not found at {Path}", databasePath);
            return;
        }

        var tempDbPath = Path.GetTempFileName();
        try
        {
            logger.LogInformation("Starting database sync to R2");

            using (var sourceDb = new SqliteConnection($"Data Source={databasePath}"))
            using (var destinationDb = new SqliteConnection($"Data Source={tempDbPath};Pooling=False;"))
            {
                sourceDb.Open();
                destinationDb.Open();
                sourceDb.BackupDatabase(destinationDb);
            }

            var dbFileName = Path.GetFileName(databasePath);
            var key = $"erpnext-dashboard/{r2.Environment}/database/{dbFileName}";
            await using var fileStream = new FileStream(tempDbPath, FileMode.Open, FileAccess.Read);
            await r2.UploadAsync(r2.BackupBucket, key, fileStream, "application/vnd.sqlite3", cancellationToken);

            logger.LogInformation("Database sync completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync database to R2");
            throw;
        }
        finally
        {
            if (File.Exists(tempDbPath))
            {
                File.Delete(tempDbPath);
            }
        }
    }
}
