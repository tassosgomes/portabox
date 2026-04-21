using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain.Unidades.Events;

public sealed record UnidadeReativadaV1(
    Guid EventId,
    Guid TenantId,
    Guid UnidadeId,
    Guid BlocoId,
    int Andar,
    string Numero,
    Guid ReativadoPorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventType => "unidade.reativada.v1";
}
