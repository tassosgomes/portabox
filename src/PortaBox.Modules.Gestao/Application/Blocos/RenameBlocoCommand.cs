using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed record RenameBlocoCommand(
    Guid CondominioId,
    Guid BlocoId,
    string Nome,
    Guid PerformedByUserId) : ICommand<BlocoDto>;
