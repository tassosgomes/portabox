using FluentValidation;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed class InativarBlocoCommandValidator : AbstractValidator<InativarBlocoCommand>
{
    public InativarBlocoCommandValidator()
    {
        RuleFor(command => command.CondominioId)
            .NotEmpty();

        RuleFor(command => command.BlocoId)
            .NotEmpty();

        RuleFor(command => command.PerformedByUserId)
            .NotEmpty();
    }
}
