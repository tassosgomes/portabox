using Amazon.S3;
using Amazon.S3.Util;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using PortaBox.Application.Abstractions.Storage;

namespace PortaBox.Api.HealthChecks;

public sealed class StorageHealthCheck(IOptions<StorageOptions> optionsAccessor) : IHealthCheck
{
    private readonly StorageOptions _options = optionsAccessor.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            if (string.Equals(_options.Provider, "S3", StringComparison.OrdinalIgnoreCase))
            {
                using var s3Client = CreateS3Client();
                var exists = await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, _options.BucketName);

                return exists
                    ? HealthCheckResult.Healthy("Object storage bucket is reachable.")
                    : HealthCheckResult.Unhealthy("Object storage bucket is not reachable.");
            }

            var minioClient = CreateMinioClient();
            var existsInMinio = await minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.BucketName),
                timeoutCts.Token);

            return existsInMinio
                ? HealthCheckResult.Healthy("Object storage bucket is reachable.")
                : HealthCheckResult.Unhealthy("Object storage bucket is not reachable.");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Object storage readiness probe timed out.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Object storage readiness probe failed.", exception);
        }
    }

    private IMinioClient CreateMinioClient()
    {
        var builder = new MinioClient()
            .WithEndpoint(NormalizeEndpoint(_options.Endpoint))
            .WithCredentials(_options.AccessKey, _options.SecretKey);

        if (_options.UseSsl)
        {
            builder = builder.WithSSL();
        }

        return builder.Build();
    }

    private IAmazonS3 CreateS3Client()
    {
        var config = new AmazonS3Config
        {
            AuthenticationRegion = string.IsNullOrWhiteSpace(_options.Region) ? "us-east-1" : _options.Region,
            ForcePathStyle = _options.ForcePathStyle
        };

        if (!string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            config.ServiceURL = _options.Endpoint;
        }

        if (_options.Endpoint is not null && Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpointUri))
        {
            config.UseHttp = endpointUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
        }

        return new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
    }

    private static string NormalizeEndpoint(string? endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return endpoint;
        }

        return uri.IsDefaultPort
            ? uri.Host
            : $"{uri.Host}:{uri.Port}";
    }
}
