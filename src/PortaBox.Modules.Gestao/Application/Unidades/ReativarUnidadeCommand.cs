using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Unidades;

public sealed record ReativarUnidadeCommand(
    Guid CondominioId,
    Guid BlocoId,
    Guid UnidadeId,
    Guid PerformedByUserId) : ICommand<UnidadeDto>;
