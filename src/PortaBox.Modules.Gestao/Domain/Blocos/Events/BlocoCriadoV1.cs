using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain.Blocos.Events;

public sealed record BlocoCriadoV1(
    Guid EventId,
    Guid TenantId,
    Guid BlocoId,
    Guid CondominioId,
    string Nome,
    Guid CriadoPorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventType => "bloco.criado.v1";
}
