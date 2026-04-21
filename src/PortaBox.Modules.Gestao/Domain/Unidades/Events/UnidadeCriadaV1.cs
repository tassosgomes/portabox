using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain.Unidades.Events;

public sealed record UnidadeCriadaV1(
    Guid EventId,
    Guid TenantId,
    Guid UnidadeId,
    Guid BlocoId,
    int Andar,
    string Numero,
    Guid CriadoPorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventType => "unidade.criada.v1";
}
