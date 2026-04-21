using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed record InativarBlocoCommand(
    Guid CondominioId,
    Guid BlocoId,
    Guid PerformedByUserId) : ICommand<BlocoDto>;
