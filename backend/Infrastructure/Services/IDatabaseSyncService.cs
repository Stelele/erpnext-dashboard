namespace Infrastructure.Services;

public interface IDatabaseSyncService
{
    Task SyncDatabaseAsync(CancellationToken cancellationToken = default);
}