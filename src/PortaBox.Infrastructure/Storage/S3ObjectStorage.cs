using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions.Storage;

namespace PortaBox.Infrastructure.Storage;

public sealed class S3ObjectStorage : IObjectStorage
{
    private readonly IAmazonS3 _client;
    private readonly StorageOptions _options;

    public S3ObjectStorage(IOptions<StorageOptions> options)
        : this(CreateClient(options), options)
    {
    }

    internal S3ObjectStorage(IAmazonS3 client, IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _options = options.Value;
    }

    public async Task<ObjectStorageReference> UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(content);

        var ownsHashingStream = content is not Sha256StreamHasher.HashingReadStream;
        var hashingStream = content as Sha256StreamHasher.HashingReadStream ?? Sha256StreamHasher.Wrap(content);

        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = hashingStream,
                ContentType = contentType,
                AutoCloseStream = false
            };

            await _client.PutObjectAsync(request, cancellationToken);

            return new ObjectStorageReference(
                key,
                contentType,
                hashingStream.BytesRead,
                hashingStream.GetComputedHashHex());
        }
        finally
        {
            if (ownsHashingStream)
            {
                await hashingStream.DisposeAsync();
            }
        }
    }

    public Task<Uri> GetDownloadUrlAsync(
        string key,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(_options.ResolvePresignedUrlTtl(ttl))
        };

        return Task.FromResult(new Uri(_client.GetPreSignedURL(request)));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
    }

    private static IAmazonS3 CreateClient(IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storageOptions = options.Value;

        ArgumentException.ThrowIfNullOrWhiteSpace(storageOptions.AccessKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageOptions.SecretKey);

        var config = new AmazonS3Config
        {
            AuthenticationRegion = string.IsNullOrWhiteSpace(storageOptions.Region) ? "us-east-1" : storageOptions.Region,
            ForcePathStyle = storageOptions.ForcePathStyle
        };

        if (!string.IsNullOrWhiteSpace(storageOptions.Endpoint))
        {
            config.ServiceURL = storageOptions.Endpoint;
        }

        if (storageOptions.Endpoint is not null &&
            Uri.TryCreate(storageOptions.Endpoint, UriKind.Absolute, out var endpointUri))
        {
            config.UseHttp = endpointUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
        }

        var credentials = new BasicAWSCredentials(storageOptions.AccessKey, storageOptions.SecretKey);

        return new AmazonS3Client(credentials, config);
    }
}
