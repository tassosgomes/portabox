using System.Text.RegularExpressions;
using FluentValidation;

namespace PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;

public sealed class CreateCondominioCommandValidator : AbstractValidator<CreateCondominioCommand>
{
    private static readonly Regex E164Regex = new("^\\+[1-9]\\d{7,14}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

    public CreateCondominioCommandValidator()
    {
        RuleFor(command => command.CreatedByUserId)
            .NotEmpty();

        RuleFor(command => command.NomeFantasia)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Cnpj)
            .NotEmpty()
            .Must(Validators.CnpjValidator.IsValid)
            .WithMessage("CNPJ must be valid.");

        RuleFor(command => command.EnderecoLogradouro)
            .MaximumLength(200);

        RuleFor(command => command.EnderecoNumero)
            .MaximumLength(20);

        RuleFor(command => command.EnderecoComplemento)
            .MaximumLength(80);

        RuleFor(command => command.EnderecoBairro)
            .MaximumLength(80);

        RuleFor(command => command.EnderecoCidade)
            .MaximumLength(80);

        RuleFor(command => command.EnderecoUf)
            .MaximumLength(2);

        RuleFor(command => command.EnderecoCep)
            .MaximumLength(8);

        RuleFor(command => command.AdministradoraNome)
            .MaximumLength(200);

        RuleFor(command => command.DataAssembleia)
            .NotEqual(default(DateOnly));

        RuleFor(command => command.QuorumDescricao)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.SignatarioNome)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.SignatarioCpf)
            .NotEmpty()
            .Must(Validators.CpfValidator.IsValid)
            .WithMessage("CPF must be valid.");

        RuleFor(command => command.DataTermo)
            .NotEqual(default(DateOnly));

        RuleFor(command => command.SindicoNomeCompleto)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.SindicoEmail)
            .NotEmpty()
            .EmailAddress();

        RuleFor(command => command.SindicoCelularE164)
            .NotEmpty()
            .Must(value => value is not null && E164Regex.IsMatch(value.Trim()))
            .WithMessage("Cell phone must be a valid E.164 number.");
    }
}
