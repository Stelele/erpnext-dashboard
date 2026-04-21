using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseSyncService : BackgroundService, IDatabaseSyncService
{
    private readonly ILogger<DatabaseSyncService> _logger;
    private readonly GoogleDriveService _googleDrive;
    private readonly TimeSpan _interval;

    public DatabaseSyncService(
        ILogger<DatabaseSyncService> logger,
        GoogleDriveService googleDrive,
        IConfiguration configuration)
    {
        _logger = logger;
        _googleDrive = googleDrive;
        var intervalMinutes = configuration.GetValue<int?>("GoogleDrive:SyncIntervalMinutes") ?? 15;
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database sync service started. Sync interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await SyncDatabaseAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Database sync service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database sync");
            }
        }
    }

    public async Task SyncDatabaseAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_googleDrive.DatabasePath))
        {
            _logger.LogWarning("Database file not found at {Path}", _googleDrive.DatabasePath);
            return;
        }

        var tempDbPath = Path.GetTempFileName();
        try
        {
            _logger.LogInformation("Starting database sync to Google Drive");

            using (var sourceDb = new SqliteConnection($"Data Source={_googleDrive.DatabasePath}"))
            using (var destinationDb = new SqliteConnection($"Data Source={tempDbPath};Pooling=False;"))
            {
                sourceDb.Open();
                destinationDb.Open();
                sourceDb.BackupDatabase(destinationDb);
            }

            using (var fileStream = new FileStream(tempDbPath, FileMode.Open, FileAccess.Read))
            {
                await _googleDrive.UploadOrUpdateFileAsync(fileStream, cancellationToken);
            }

            _logger.LogInformation("Database sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync database to Google Drive");
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