using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain.Blocos.Events;

public sealed record BlocoReativadoV1(
    Guid EventId,
    Guid TenantId,
    Guid BlocoId,
    string Nome,
    Guid ReativadoPorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventType => "bloco.reativado.v1";
}
