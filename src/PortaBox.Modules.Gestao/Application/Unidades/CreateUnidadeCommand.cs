using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Unidades;

public sealed record CreateUnidadeCommand(
    Guid CondominioId,
    Guid BlocoId,
    int Andar,
    string Numero,
    Guid PerformedByUserId) : ICommand<UnidadeDto>;
