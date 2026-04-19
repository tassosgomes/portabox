using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Commands.ResendMagicLink;

public sealed record ResendMagicLinkCommand(
    Guid CondominioId,
    Guid SindicoUserId,
    Guid PerformedByUserId) : ICommand<ResendMagicLinkResult>;
