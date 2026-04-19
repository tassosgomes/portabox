using FluentValidation;
using Microsoft.Extensions.Logging;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Application.Abstractions.Storage;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Commands.UploadOptInDocument;

public sealed class UploadOptInDocumentCommandHandler(
    IValidator<UploadOptInDocumentCommand> validator,
    ICondominioRepository condominioRepository,
    IObjectStorage objectStorage,
    IOptInDocumentRepository optInDocumentRepository,
    IApplicationDbSession dbSession,
    ILogger<UploadOptInDocumentCommandHandler> logger,
    TimeProvider timeProvider) : ICommandHandler<UploadOptInDocumentCommand, UploadOptInDocumentResult>
{
    public async Task<Result<UploadOptInDocumentResult>> HandleAsync(UploadOptInDocumentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var tenant = await condominioRepository.GetByIdAsync(command.TenantId, cancellationToken);

        if (tenant is null)
        {
            return Result<UploadOptInDocumentResult>.Failure(UploadOptInDocumentErrors.TenantNotFound);
        }

        if (tenant.Status is not (CondominioStatus.PreAtivo or CondominioStatus.Ativo))
        {
            return Result<UploadOptInDocumentResult>.Failure(UploadOptInDocumentErrors.TenantNotEligibleForOptInDocumentUpload);
        }

        var documentId = Guid.NewGuid();
        var storageKey = ObjectStorageKeyFactory.BuildOptInDocumentKey(command.TenantId, documentId, command.ContentType);

        await using var hashingStream = Sha256StreamHasher.Wrap(command.Stream);
        await objectStorage.UploadAsync(storageKey, hashingStream, command.ContentType, cancellationToken);

        var document = OptInDocument.Create(
            documentId,
            command.TenantId,
            command.Kind,
            storageKey,
            command.ContentType,
            hashingStream.BytesRead,
            hashingStream.GetComputedHashHex(),
            command.UploadedByUserId,
            timeProvider);

        await optInDocumentRepository.AddAsync(document, cancellationToken);

        try
        {
            await dbSession.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "storage.orphan-candidate storage_key={StorageKey} tenant_id={TenantId} document_id={DocumentId}",
                storageKey,
                command.TenantId,
                documentId);

            throw;
        }

        return Result<UploadOptInDocumentResult>.Success(new UploadOptInDocumentResult(documentId));
    }
}
