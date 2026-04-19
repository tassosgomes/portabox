using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Queries.ListCondominios;

public sealed record CondominioListItemDto(
    Guid Id,
    string NomeFantasia,
    string CnpjMasked,
    CondominioStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ActivatedAt);
