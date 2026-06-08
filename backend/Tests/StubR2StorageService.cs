using Infrastructure.Services;

namespace Tests;

public class StubR2StorageService : IR2StorageService
{
    public string BackupBucket => "test-backup";
    public string Environment => "test";

    public Task UploadAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct = default)
    {
        content.Dispose();
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default) =>
        Task.FromResult<Stream>(new MemoryStream());

    public Task DeleteAsync(string bucket, string key, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default) =>
        Task.FromResult(true);
}
