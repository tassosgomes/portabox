namespace PortaBox.Application.Abstractions.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Minio";

    public string BucketName { get; set; } = "log-portaria-dev";

    public string? Endpoint { get; set; }

    public string? Region { get; set; } = "us-east-1";

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public bool UseSsl { get; set; }

    public bool ForcePathStyle { get; set; } = true;

    public int PresignedUrlTtlMinutes { get; set; } = 5;

    public TimeSpan ResolvePresignedUrlTtl(TimeSpan? overrideTtl = null)
    {
        if (overrideTtl is { } ttl)
        {
            return ttl;
        }

        if (PresignedUrlTtlMinutes <= 0)
        {
            return TimeSpan.FromMinutes(5);
        }

        return TimeSpan.FromMinutes(PresignedUrlTtlMinutes);
    }
}
