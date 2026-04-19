using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Queries.GetCondominioDetails;

public sealed record CondominioDetailsDto(
    Guid Id,
    string NomeFantasia,
    string CnpjMasked,
    CondominioStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ActivatedAt,
    string? EnderecoLogradouro,
    string? EnderecoNumero,
    string? EnderecoComplemento,
    string? EnderecoBairro,
    string? EnderecoCidade,
    string? EnderecoUf,
    string? EnderecoCep,
    string? AdministradoraNome,
    CondominioOptInDetailsDto? OptIn,
    CondominioSindicoDetailsDto? Sindico,
    IReadOnlyCollection<CondominioDocumentDetailsDto> Documentos,
    IReadOnlyCollection<CondominioAuditLogItemDto> AuditLog,
    bool SindicoSenhaDefinida);

public sealed record CondominioOptInDetailsDto(
    DateOnly DataAssembleia,
    string QuorumDescricao,
    string SignatarioNome,
    string SignatarioCpfMasked,
    DateOnly DataTermo,
    Guid RegisteredByUserId,
    DateTimeOffset RegisteredAt);

public sealed record CondominioSindicoDetailsDto(
    Guid Id,
    Guid UserId,
    string NomeCompleto,
    string Email,
    string CelularMasked,
    SindicoStatus Status,
    DateTimeOffset CreatedAt);

public sealed record CondominioDocumentDetailsDto(
    Guid Id,
    OptInDocumentKind Kind,
    string ContentType,
    long SizeBytes,
    string Sha256,
    DateTimeOffset UploadedAt,
    Guid UploadedByUserId);

public sealed record CondominioAuditLogItemDto(
    long Id,
    TenantAuditEventKind EventKind,
    Guid PerformedByUserId,
    DateTimeOffset OccurredAt,
    string? Note,
    string? MetadataJson);
