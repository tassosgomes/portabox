using FluentValidation;

namespace PortaBox.Modules.Gestao.Application.Commands.ActivateCondominio;

public sealed class ActivateCondominioCommandValidator : AbstractValidator<ActivateCondominioCommand>
{
    public const int MaxNoteLength = 500;

    public ActivateCondominioCommandValidator()
    {
        RuleFor(command => command.CondominioId)
            .NotEmpty();

        RuleFor(command => command.PerformedByUserId)
            .NotEmpty();

        RuleFor(command => command.Note)
            .MaximumLength(MaxNoteLength);
    }
}
