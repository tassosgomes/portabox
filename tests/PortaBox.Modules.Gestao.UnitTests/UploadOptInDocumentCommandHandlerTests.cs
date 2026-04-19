using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Application.Abstractions.Storage;
using PortaBox.Modules.Gestao.Application.Commands.UploadOptInDocument;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class UploadOptInDocumentCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldComputeSha256ForKnownStream()
    {
        var validator = new UploadOptInDocumentCommandValidator();
        var tenant = CreateTenant();
        var condominioRepository = new Mock<ICondominioRepository>();
        condominioRepository
            .Setup(repository => repository.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        OptInDocument? persistedDocument = null;
        var optInDocumentRepository = new Mock<IOptInDocumentRepository>();
        optInDocumentRepository
            .Setup(repository => repository.AddAsync(It.IsAny<OptInDocument>(), It.IsAny<CancellationToken>()))
            .Callback<OptInDocument, CancellationToken>((document, _) => persistedDocument = document)
            .Returns(Task.CompletedTask);

        var dbSession = new Mock<IApplicationDbSession>();
        dbSession
            .Setup(session => session.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var storage = new CapturingObjectStorage();
        var handler = BuildHandler(validator, condominioRepository, storage, optInDocumentRepository, dbSession);
        var payload = "PortaBox opt-in"u8.ToArray();
        await using var stream = new MemoryStream(payload);
        var command = BuildCommand(tenant.Id, stream, payload.LongLength);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(persistedDocument);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(payload)).ToLowerInvariant(), persistedDocument!.Sha256);
        Assert.Equal(payload.LongLength, persistedDocument.SizeBytes);
        Assert.Equal(payload, storage.UploadedPayload);
    }

    [Fact]
    public async Task HandleAsync_TenantDoesNotExist_ShouldReturnFailure()
    {
        var condominioRepository = new Mock<ICondominioRepository>();
        condominioRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Condominio?)null);

        var handler = BuildHandler(
            new UploadOptInDocumentCommandValidator(),
            condominioRepository,
            new CapturingObjectStorage(),
            new Mock<IOptInDocumentRepository>(),
            new Mock<IApplicationDbSession>());

        var result = await handler.HandleAsync(BuildCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UploadOptInDocumentErrors.TenantNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenUploadFails_ShouldNotTouchDatabase()
    {
        var tenant = CreateTenant();
        var condominioRepository = new Mock<ICondominioRepository>();
        condominioRepository
            .Setup(repository => repository.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var optInDocumentRepository = new Mock<IOptInDocumentRepository>();
        var dbSession = new Mock<IApplicationDbSession>();
        var storage = new FailingObjectStorage();
        var handler = BuildHandler(
            new UploadOptInDocumentCommandValidator(),
            condominioRepository,
            storage,
            optInDocumentRepository,
            dbSession);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(BuildCommand(tenant.Id), CancellationToken.None));

        optInDocumentRepository.Verify(repository => repository.AddAsync(It.IsAny<OptInDocument>(), It.IsAny<CancellationToken>()), Times.Never);
        dbSession.Verify(session => session.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static UploadOptInDocumentCommandHandler BuildHandler(
        IValidator<UploadOptInDocumentCommand> validator,
        Mock<ICondominioRepository> condominioRepository,
        IObjectStorage objectStorage,
        Mock<IOptInDocumentRepository> optInDocumentRepository,
        Mock<IApplicationDbSession> dbSession,
        TimeProvider? timeProvider = null)
    {
        return new UploadOptInDocumentCommandHandler(
            validator,
            condominioRepository.Object,
            objectStorage,
            optInDocumentRepository.Object,
            dbSession.Object,
            NullLogger<UploadOptInDocumentCommandHandler>.Instance,
            timeProvider ?? TimeProvider.System);
    }

    private static UploadOptInDocumentCommand BuildCommand(Guid? tenantId = null, Stream? stream = null, long sizeBytes = 4)
    {
        return new UploadOptInDocumentCommand(
            tenantId ?? Guid.NewGuid(),
            OptInDocumentKind.Ata,
            "application/pdf",
            "ata.pdf",
            sizeBytes,
            stream ?? new MemoryStream([1, 2, 3, 4]),
            Guid.NewGuid());
    }

    private static Condominio CreateTenant()
    {
        return Condominio.Create(
            Guid.NewGuid(),
            "Residencial Bosque Azul",
            "12.345.678/0001-95",
            Guid.NewGuid(),
            TimeProvider.System);
    }

    private sealed class CapturingObjectStorage : IObjectStorage
    {
        public byte[] UploadedPayload { get; private set; } = [];

        public async Task<ObjectStorageReference> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            await using var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream, cancellationToken);
            UploadedPayload = memoryStream.ToArray();

            return new ObjectStorageReference(key, contentType, UploadedPayload.LongLength, new string('0', 64));
        }

        public Task<Uri> GetDownloadUrlAsync(string key, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FailingObjectStorage : IObjectStorage
    {
        public Task<ObjectStorageReference> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Upload failed.");
        }

        public Task<Uri> GetDownloadUrlAsync(string key, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
