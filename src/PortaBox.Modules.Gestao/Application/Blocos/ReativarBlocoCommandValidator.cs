using FluentValidation;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed class ReativarBlocoCommandValidator : AbstractValidator<ReativarBlocoCommand>
{
    public ReativarBlocoCommandValidator()
    {
        RuleFor(command => command.CondominioId)
            .NotEmpty();

        RuleFor(command => command.BlocoId)
            .NotEmpty();

        RuleFor(command => command.PerformedByUserId)
            .NotEmpty();
    }
}
