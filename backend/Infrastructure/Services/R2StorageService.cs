using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class R2StorageService : IR2StorageService
{
    private readonly AmazonS3Client _s3Client;
    private readonly string _backupBucket;
    private readonly string _publicBucket;

    public string BackupBucket => _backupBucket;
    public string PublicBucket => _publicBucket;
    public string Environment { get; }

    public R2StorageService(IConfiguration configuration)
    {
        var accountId = configuration["R2:AccountId"] ?? throw new InvalidOperationException("R2:AccountId is required");
        var accessKeyId = configuration["R2:AccessKeyId"] ?? throw new InvalidOperationException("R2:AccessKeyId is required");
        var secretAccessKey = configuration["R2:SecretAccessKey"] ?? throw new InvalidOperationException("R2:SecretAccessKey is required");
        _backupBucket = configuration["R2:BackupBucketName"] ?? throw new InvalidOperationException("R2:BackupBucketName is required");
        _publicBucket = configuration["R2:PublicBucketName"] ?? throw new InvalidOperationException("R2:PublicBucketName is required");

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
        };

        _s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, config);

        var aspnetEnv = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment = string.Equals(aspnetEnv, "Production", StringComparison.OrdinalIgnoreCase) ? "prod" : "local";
    }

    public async Task UploadAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
        };

        await _s3Client.PutObjectAsync(request, ct);
    }

    public async Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = bucket,
            Key = key,
        };

        var response = await _s3Client.GetObjectAsync(request, ct);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string bucket, string key, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = bucket,
            Key = key,
        };

        await _s3Client.DeleteObjectAsync(request, ct);
    }

    public async Task<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucket,
                Key = key,
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, ct);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
