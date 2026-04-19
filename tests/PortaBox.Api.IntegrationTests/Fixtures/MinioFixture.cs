using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions.Storage;

namespace PortaBox.Api.IntegrationTests.Fixtures;

public sealed class MinioFixture : IAsyncLifetime
{
    private const ushort ApiPort = 9000;
    private const ushort ConsolePort = 9001;
    private const string DefaultAccessKey = "minioadmin";
    private const string DefaultSecretKey = "minioadmin";
    private const string DefaultBucketName = "log-portaria-integration";

    private readonly IContainer _container = new ContainerBuilder("minio/minio:RELEASE.2025-02-28T09-55-16Z")
        .WithPortBinding(ApiPort, true)
        .WithPortBinding(ConsolePort, true)
        .WithEnvironment("MINIO_ROOT_USER", DefaultAccessKey)
        .WithEnvironment("MINIO_ROOT_PASSWORD", DefaultSecretKey)
        .WithCommand("server", "/data", "--console-address", $":{ConsolePort}")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(ApiPort))
        .Build();

    public string BucketName => DefaultBucketName;

    public string Endpoint => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(ApiPort)}";

    public Task InitializeAsync() => InitializeCoreAsync();

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public StorageOptions CreateOptions()
    {
        return new StorageOptions
        {
            Provider = "Minio",
            BucketName = BucketName,
            Endpoint = Endpoint,
            AccessKey = DefaultAccessKey,
            SecretKey = DefaultSecretKey,
            UseSsl = false,
            ForcePathStyle = true,
            Region = "us-east-1",
            PresignedUrlTtlMinutes = 5
        };
    }

    public IOptions<StorageOptions> CreateOptionsAccessor() => Microsoft.Extensions.Options.Options.Create(CreateOptions());

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateS3Client();
        string? continuationToken = null;

        do
        {
            var response = await client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName,
                ContinuationToken = continuationToken
            }, cancellationToken);

            if (response.S3Objects.Count > 0)
            {
                await client.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = BucketName,
                    Objects = response.S3Objects.Select(item => new KeyVersion { Key = item.Key }).ToList()
                }, cancellationToken);
            }

            continuationToken = response.IsTruncated ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);
    }

    public AmazonS3Client CreateS3Client()
    {
        var credentials = new BasicAWSCredentials(DefaultAccessKey, DefaultSecretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = Endpoint,
            UseHttp = true,
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1"
        };

        return new AmazonS3Client(credentials, config);
    }

    private async Task InitializeCoreAsync()
    {
        await _container.StartAsync();

        using var client = CreateS3Client();

        var buckets = await client.ListBucketsAsync();
        if (buckets.Buckets.Any(bucket => bucket.BucketName == BucketName))
        {
            return;
        }

        await client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = BucketName,
            UseClientRegion = true
        });
    }
}
