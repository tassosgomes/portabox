using System.Text.RegularExpressions;
using FluentValidation;

namespace PortaBox.Modules.Gestao.Application.Unidades;

public sealed class CreateUnidadeCommandValidator : AbstractValidator<CreateUnidadeCommand>
{
    private static readonly Regex NumeroPattern = new("^[0-9]{1,4}[A-Za-z]?$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public CreateUnidadeCommandValidator()
    {
        RuleFor(command => command.CondominioId)
            .NotEmpty();

        RuleFor(command => command.BlocoId)
            .NotEmpty();

        RuleFor(command => command.PerformedByUserId)
            .NotEmpty();

        RuleFor(command => command.Andar)
            .GreaterThanOrEqualTo(0)
            .WithMessage("O andar da unidade deve ser maior ou igual a zero.");

        RuleFor(command => command.Numero)
            .Must(BeValidNumero)
            .WithMessage("O numero da unidade deve seguir o formato de 1 a 4 digitos com sufixo alfabetico opcional.");
    }

    private static bool BeValidNumero(string? numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            return false;
        }

        return NumeroPattern.IsMatch(numero.Trim());
    }
}
