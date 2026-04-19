using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain.Events;

public sealed record CondominioAtivadoV1(
    Guid CondominioId,
    Guid ActivatedByUserId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventType => "condominio.ativado.v1";
}
