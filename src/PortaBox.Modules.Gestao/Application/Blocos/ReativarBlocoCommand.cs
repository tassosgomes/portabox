using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed record ReativarBlocoCommand(
    Guid CondominioId,
    Guid BlocoId,
    Guid PerformedByUserId) : ICommand<BlocoDto>;
