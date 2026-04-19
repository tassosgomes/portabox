using System.Net;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.Storage;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MultiTenancy;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Infrastructure.Repositories;
using PortaBox.Infrastructure.Storage;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class ObjectStorageAndOptInDocumentIntegrationTests(
    PostgresDatabaseFixture postgresFixture,
    MinioFixture minioFixture) : IClassFixture<MinioFixture>
{
    [Fact]
    public async Task UploadAsync_ShouldStorePdfInMinioBucket()
    {
        await minioFixture.ResetAsync();

        var storage = new MinioObjectStorage(minioFixture.CreateOptionsAccessor());
        var tenantId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var key = ObjectStorageKeyFactory.BuildOptInDocumentKey(tenantId, documentId, "application/pdf");
        var payload = Enumerable.Repeat((byte)0x2A, 1024 * 1024).ToArray();
        await using var stream = new MemoryStream(payload);
        using var client = minioFixture.CreateS3Client();

        var reference = await storage.UploadAsync(key, stream, "application/pdf");
        using var response = await client.GetObjectAsync(minioFixture.BucketName, key);
        await using var downloaded = new MemoryStream();
        await response.ResponseStream.CopyToAsync(downloaded);

        Assert.Equal(key, reference.Key);
        Assert.Equal(payload.LongLength, reference.SizeBytes);
        Assert.Equal(payload, downloaded.ToArray());
    }

    [Fact]
    public async Task GetDownloadUrlAsync_ShouldReturnPresignedUrlWithConfiguredFiveMinuteTtl()
    {
        await minioFixture.ResetAsync();

        var storage = new MinioObjectStorage(minioFixture.CreateOptionsAccessor());
        var key = ObjectStorageKeyFactory.BuildOptInDocumentKey(Guid.NewGuid(), Guid.NewGuid(), "application/pdf");
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        await storage.UploadAsync(key, stream, "application/pdf");
        var url = await storage.GetDownloadUrlAsync(key);

        Assert.Equal(Uri.UriSchemeHttp, url.Scheme);
        Assert.Contains("X-Amz-Algorithm", url.Query, StringComparison.Ordinal);
        Assert.Contains("X-Amz-Credential", url.Query, StringComparison.Ordinal);
        Assert.Contains("X-Amz-Expires=300", url.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveObjectFromBucket()
    {
        await minioFixture.ResetAsync();

        var storage = new MinioObjectStorage(minioFixture.CreateOptionsAccessor());
        var key = ObjectStorageKeyFactory.BuildOptInDocumentKey(Guid.NewGuid(), Guid.NewGuid(), "application/pdf");
        await using var stream = new MemoryStream(new byte[] { 9, 8, 7, 6 });
        using var client = minioFixture.CreateS3Client();

        await storage.UploadAsync(key, stream, "application/pdf");
        await storage.DeleteAsync(key);

        var exception = await Assert.ThrowsAsync<AmazonS3Exception>(() => client.GetObjectAsync(minioFixture.BucketName, key));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task S3ObjectStorage_ShouldUploadAndDeleteObjectAgainstS3CompatibleEndpoint()
    {
        await minioFixture.ResetAsync();

        var options = minioFixture.CreateOptions();
        options.Provider = "S3";
        var storage = new S3ObjectStorage(Microsoft.Extensions.Options.Options.Create(options));
        var key = ObjectStorageKeyFactory.BuildOptInDocumentKey(Guid.NewGuid(), Guid.NewGuid(), "image/png");
        var payload = new byte[] { 10, 20, 30, 40, 50 };
        await using var stream = new MemoryStream(payload);
        using var client = minioFixture.CreateS3Client();

        var reference = await storage.UploadAsync(key, stream, "image/png");
        var presignedUrl = await storage.GetDownloadUrlAsync(key);

        using (var response = await client.GetObjectAsync(minioFixture.BucketName, key))
        {
            await using var downloaded = new MemoryStream();
            await response.ResponseStream.CopyToAsync(downloaded);
            Assert.Equal(payload, downloaded.ToArray());
        }

        Assert.Equal("image/png", reference.ContentType);
        Assert.True(
            presignedUrl.Query.Contains("X-Amz-Expires=300", StringComparison.Ordinal) ||
            presignedUrl.Query.Contains("Expires=", StringComparison.Ordinal),
            $"Unexpected presigned URL query: {presignedUrl.Query}");

        await storage.DeleteAsync(key);

        var exception = await Assert.ThrowsAsync<AmazonS3Exception>(() => client.GetObjectAsync(minioFixture.BucketName, key));
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task OptInDocumentRepository_ShouldPersistAndRespectTenantIsolation()
    {
        await postgresFixture.ResetAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        await using (var seedContext = BuildContext())
        {
            var user = await CreateUserAsync(seedContext, "operator-opt-in-document");

            seedContext.Condominios.AddRange(
                Condominio.Create(tenantA, "Condominio A", "12.345.678/0001-95", user.Id, TimeProvider.System),
                Condominio.Create(tenantB, "Condominio B", "45.723.174/0001-10", user.Id, TimeProvider.System));

            var repository = new OptInDocumentRepository(seedContext);
            await repository.AddAsync(OptInDocument.Create(
                documentId,
                tenantA,
                OptInDocumentKind.Ata,
                ObjectStorageKeyFactory.BuildOptInDocumentKey(tenantA, documentId, "application/pdf"),
                "application/pdf",
                4096,
                new string('c', 64),
                user.Id,
                TimeProvider.System));

            await seedContext.SaveChangesAsync();
        }

        var tenantContext = new TenantContext();
        using var scope = tenantContext.BeginScope(tenantB);

        await using var tenantDbContext = BuildContext(tenantContext);
        var tenantRepository = new OptInDocumentRepository(tenantDbContext);
        var visibleToTenantB = await tenantDbContext.OptInDocuments.ToListAsync();
        var hiddenDocument = await tenantRepository.GetByIdAsync(documentId);

        Assert.Empty(visibleToTenantB);
        Assert.Null(hiddenDocument);
    }

    [Fact]
    public async Task Migration_ShouldCreateOptInDocumentTableWithCompositeDescendingIndex()
    {
        await postgresFixture.ResetAsync();
        await using var connection = await postgresFixture.OpenConnectionAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT indexdef
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename = 'opt_in_document'
              AND indexname = 'idx_opt_in_document_tenant_id_uploaded_at_desc';
            """,
            connection);

        var scalar = await command.ExecuteScalarAsync();

        Assert.NotNull(scalar);
        Assert.Contains("(tenant_id, uploaded_at DESC)", scalar!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private AppDbContext BuildContext(TenantContext? tenantContext = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(postgresFixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    private static async Task<AppUser> CreateUserAsync(AppDbContext dbContext, string slug)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{slug}@portabox.test",
            NormalizedUserName = $"{slug}@PORTABOX.TEST",
            Email = $"{slug}@portabox.test",
            NormalizedEmail = $"{slug}@PORTABOX.TEST",
            EmailConfirmed = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}
