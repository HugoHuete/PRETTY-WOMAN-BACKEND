using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Media;

public class R2MediaObjectStorage(IOptions<R2MediaOptions> options) : IMediaObjectStorage, IDisposable
{
    private readonly R2MediaOptions _options = options.Value;
    private AmazonS3Client? _client;

    public async Task UploadAsync(MediaBucket bucket, string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = GetBucketName(bucket),
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };
        await GetClient().PutObjectAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(MediaBucket bucket, string storageKey, CancellationToken cancellationToken = default)
    {
        await GetClient().DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = GetBucketName(bucket),
            Key = storageKey
        }, cancellationToken);
    }

    public void Dispose() => _client?.Dispose();

    private AmazonS3Client GetClient()
    {
        EnsureConfigured();
        return _client ??= new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,
                AuthenticationRegion = "auto",
                ForcePathStyle = true
            });
    }

    private string GetBucketName(MediaBucket bucket)
    {
        EnsureConfigured();
        return bucket == MediaBucket.Public ? _options.PublicBucketName : _options.PrivateBucketName;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ServiceUrl) ||
            string.IsNullOrWhiteSpace(_options.AccessKeyId) ||
            string.IsNullOrWhiteSpace(_options.SecretAccessKey) ||
            string.IsNullOrWhiteSpace(_options.PublicBucketName) ||
            string.IsNullOrWhiteSpace(_options.PrivateBucketName))
        {
            throw new InvalidOperationException("La configuración R2Media está incompleta.");
        }
    }
}
