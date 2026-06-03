namespace Infrastructure.Services;

public interface IR2StorageService
{
    string BackupBucket { get; }
    string Environment { get; }
    Task UploadAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default);
    Task DeleteAsync(string bucket, string key, CancellationToken ct = default);
    Task<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default);
}
