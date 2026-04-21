using FluentValidation;

namespace PortaBox.Modules.Gestao.Application.Unidades;

public sealed class InativarUnidadeCommandValidator : AbstractValidator<InativarUnidadeCommand>
{
    public InativarUnidadeCommandValidator()
    {
        RuleFor(command => command.CondominioId)
            .NotEmpty();

        RuleFor(command => command.BlocoId)
            .NotEmpty();

        RuleFor(command => command.UnidadeId)
            .NotEmpty();

        RuleFor(command => command.PerformedByUserId)
            .NotEmpty();
    }
}
