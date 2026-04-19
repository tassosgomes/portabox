using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Commands.ActivateCondominio;

public sealed record ActivateCondominioCommand(
    Guid CondominioId,
    Guid PerformedByUserId,
    string? Note) : ICommand<ActivateCondominioResult>;
