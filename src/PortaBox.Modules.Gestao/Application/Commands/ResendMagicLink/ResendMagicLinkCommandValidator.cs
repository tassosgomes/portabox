using FluentValidation;

namespace PortaBox.Modules.Gestao.Application.Commands.ResendMagicLink;

public sealed class ResendMagicLinkCommandValidator : AbstractValidator<ResendMagicLinkCommand>
{
    public ResendMagicLinkCommandValidator()
    {
        RuleFor(command => command.CondominioId)
            .NotEmpty();

        RuleFor(command => command.SindicoUserId)
            .NotEmpty();

        RuleFor(command => command.PerformedByUserId)
            .NotEmpty();
    }
}
