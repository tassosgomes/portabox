using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain.Events;

public sealed record CondominioCadastradoV1(
    Guid CondominioId,
    Guid SindicoUserId,
    string NomeFantasia,
    string Cnpj,
    string SindicoNomeCompleto,
    string SindicoEmail,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventType => "condominio.cadastrado.v1";
}
