using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed record CreateBlocoCommand(
    Guid CondominioId,
    string Nome,
    Guid PerformedByUserId) : ICommand<BlocoDto>;
