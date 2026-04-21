using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain.Blocos.Events;

public sealed record BlocoInativadoV1(
    Guid EventId,
    Guid TenantId,
    Guid BlocoId,
    string Nome,
    Guid InativadoPorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventType => "bloco.inativado.v1";
}
