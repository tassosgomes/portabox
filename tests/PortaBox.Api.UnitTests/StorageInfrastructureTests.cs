using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions.Storage;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Storage;

namespace PortaBox.Api.UnitTests;

public sealed class StorageInfrastructureTests
{
    [Fact]
    public async Task Sha256StreamHasher_ShouldComputeHashWithoutPreConsumingUnderlyingStream()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("PortaBox opt-in payload");
        var expectedHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        await using var sourceStream = new MemoryStream(payload);
        await using var hashingStream = Sha256StreamHasher.Wrap(sourceStream);
        await using var copyTarget = new MemoryStream();

        await hashingStream.CopyToAsync(copyTarget);

        Assert.Equal(payload, copyTarget.ToArray());
        Assert.Equal(payload.Length, hashingStream.BytesRead);
        Assert.Equal(payload.Length, sourceStream.Position);
        Assert.Equal(expectedHash, hashingStream.GetComputedHashHex());
    }

    [Fact]
    public async Task Sha256StreamHasher_ShouldSupportSeekAndAsyncReadOverloads()
    {
        var payload = Enumerable.Range(1, 16).Select(value => (byte)value).ToArray();
        await using var sourceStream = new MemoryStream(payload);
        await using var hashingStream = Sha256StreamHasher.Wrap(sourceStream);
        var firstBuffer = new byte[4];

        var firstRead = hashingStream.Read(firstBuffer, 0, firstBuffer.Length);
        hashingStream.Seek(0, SeekOrigin.Begin);
        sourceStream.Seek(0, SeekOrigin.Begin);
        var secondBuffer = new byte[payload.Length];
        var secondRead = await hashingStream.ReadAsync(secondBuffer, 0, secondBuffer.Length, CancellationToken.None);

        Assert.Equal(4, firstRead);
        Assert.Equal(payload.Length, secondRead);
        Assert.Equal(payload, secondBuffer);
    }

    [Fact]
    public void ObjectStorageKeyFactory_ShouldBuildOptInDocumentKeyUsingAdrConvention()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var documentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var key = ObjectStorageKeyFactory.BuildOptInDocumentKey(tenantId, documentId, "application/pdf");

        Assert.Equal(
            "condominios/11111111-1111-1111-1111-111111111111/opt-in/22222222-2222-2222-2222-222222222222.pdf",
            key);
    }

    [Fact]
    public void ObjectStorageKeyFactory_ShouldRejectUnsupportedContentType()
    {
        var exception = Assert.Throws<ArgumentException>(() => ObjectStorageKeyFactory.BuildOptInDocumentKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "text/plain"));

        Assert.Equal("contentType", exception.ParamName);
    }

    [Fact]
    public void ObjectStorageReference_ShouldRejectNegativeSize()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectStorageReference(
            "condominios/test/opt-in/doc.pdf",
            "application/pdf",
            -1,
            new string('a', 64)));

        Assert.Equal("sizeBytes", exception.ParamName);
    }

    [Fact]
    public void StorageOptions_ShouldResolveDefaultAndOverrideTtl()
    {
        var options = new StorageOptions
        {
            PresignedUrlTtlMinutes = 0
        };

        Assert.Equal(TimeSpan.FromMinutes(5), options.ResolvePresignedUrlTtl());
        Assert.Equal(TimeSpan.FromMinutes(2), options.ResolvePresignedUrlTtl(TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterMinioObjectStorageWhenProviderIsMinio()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=portabox_storage_minio;Username=postgres;Password=postgres",
            ["Storage:Provider"] = "Minio",
            ["Storage:BucketName"] = "log-portaria-test",
            ["Storage:Endpoint"] = "http://localhost:9000",
            ["Storage:AccessKey"] = "minioadmin",
            ["Storage:SecretKey"] = "minioadmin",
            ["Storage:UseSsl"] = "false"
        });

        using var scope = provider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<StorageOptions>>();

        Assert.IsType<MinioObjectStorage>(storage);
        Assert.Equal("Minio", options.Value.Provider);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterS3ObjectStorageWhenProviderIsS3()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=portabox_storage_s3;Username=postgres;Password=postgres",
            ["Storage:Provider"] = "S3",
            ["Storage:BucketName"] = "log-portaria-test",
            ["Storage:Endpoint"] = "https://r2.example.test",
            ["Storage:AccessKey"] = "test-access-key",
            ["Storage:SecretKey"] = "test-secret-key",
            ["Storage:Region"] = "auto",
            ["Storage:UseSsl"] = "true",
            ["Storage:ForcePathStyle"] = "false"
        });

        using var scope = provider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<StorageOptions>>();

        Assert.IsType<S3ObjectStorage>(storage);
        Assert.Equal("S3", options.Value.Provider);
    }

    private static ServiceProvider BuildServiceProvider(Dictionary<string, string?> values)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
