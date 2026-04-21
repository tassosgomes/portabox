using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain.Unidades.Events;

public sealed record UnidadeInativadaV1(
    Guid EventId,
    Guid TenantId,
    Guid UnidadeId,
    Guid BlocoId,
    int Andar,
    string Numero,
    Guid InativadoPorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventType => "unidade.inativada.v1";
}
