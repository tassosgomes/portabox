using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Commands.UploadOptInDocument;

public sealed record UploadOptInDocumentCommand(
    Guid TenantId,
    OptInDocumentKind Kind,
    string ContentType,
    string FileName,
    long SizeBytes,
    Stream Stream,
    Guid UploadedByUserId) : ICommand<UploadOptInDocumentResult>;
