using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using PortaBox.Application.Abstractions.Storage;

namespace PortaBox.Infrastructure.Storage;

public sealed class MinioObjectStorage : IObjectStorage
{
    private readonly IMinioClient _client;
    private readonly StorageOptions _options;

    public MinioObjectStorage(IOptions<StorageOptions> options)
        : this(CreateClient(options), options)
    {
    }

    internal MinioObjectStorage(IMinioClient client, IOptions<StorageOptions> options)
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

        await EnsureBucketExistsAsync(cancellationToken);

        var ownsHashingStream = content is not Sha256StreamHasher.HashingReadStream;
        var hashingStream = content as Sha256StreamHasher.HashingReadStream ?? Sha256StreamHasher.Wrap(content);

        try
        {
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(key)
                .WithStreamData(hashingStream)
                .WithObjectSize(GetObjectSize(content))
                .WithContentType(contentType);

            await _client.PutObjectAsync(putObjectArgs, cancellationToken);

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

    public async Task<Uri> GetDownloadUrlAsync(
        string key,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        var presignedUrl = await _client.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(key)
                .WithExpiry((int)_options.ResolvePresignedUrlTtl(ttl).TotalSeconds));

        return new Uri(presignedUrl);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _client.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(key),
            cancellationToken);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_options.BucketName),
            cancellationToken);

        if (bucketExists)
        {
            return;
        }

        await _client.MakeBucketAsync(
            new MakeBucketArgs().WithBucket(_options.BucketName),
            cancellationToken);
    }

    private static IMinioClient CreateClient(IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storageOptions = options.Value;

        ArgumentException.ThrowIfNullOrWhiteSpace(storageOptions.Endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageOptions.AccessKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageOptions.SecretKey);

        var builder = new MinioClient()
            .WithEndpoint(NormalizeEndpoint(storageOptions.Endpoint))
            .WithCredentials(storageOptions.AccessKey, storageOptions.SecretKey);

        if (storageOptions.UseSsl)
        {
            builder = builder.WithSSL();
        }

        return builder.Build();
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return endpoint;
        }

        return uri.IsDefaultPort
            ? uri.Host
            : $"{uri.Host}:{uri.Port}";
    }

    private static long GetObjectSize(Stream content)
    {
        return content.CanSeek
            ? content.Length - content.Position
            : -1;
    }
}
