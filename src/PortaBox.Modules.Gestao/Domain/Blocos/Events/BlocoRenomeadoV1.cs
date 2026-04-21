using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain.Blocos.Events;

public sealed record BlocoRenomeadoV1(
    Guid EventId,
    Guid TenantId,
    Guid BlocoId,
    string NomeAntes,
    string NomeDepois,
    Guid RenomeadoPorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventType => "bloco.renomeado.v1";
}
