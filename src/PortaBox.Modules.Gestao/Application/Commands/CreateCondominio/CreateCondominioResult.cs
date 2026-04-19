namespace PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;

public sealed record CreateCondominioResult(
    Guid CondominioId,
    Guid SindicoUserId);
