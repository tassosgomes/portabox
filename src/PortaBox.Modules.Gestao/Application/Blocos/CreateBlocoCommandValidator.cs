using FluentValidation;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed class CreateBlocoCommandValidator : AbstractValidator<CreateBlocoCommand>
{
    public CreateBlocoCommandValidator()
    {
        RuleFor(command => command.CondominioId)
            .NotEmpty();

        RuleFor(command => command.PerformedByUserId)
            .NotEmpty();

        RuleFor(command => command.Nome)
            .Must(HasTrimmedName)
            .WithMessage("O nome do bloco deve ter entre 1 e 50 caracteres.");
    }

    private static bool HasTrimmedName(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return false;
        }

        var normalizedNome = nome.Trim();
        return normalizedNome.Length is >= 1 and <= 50;
    }
}
