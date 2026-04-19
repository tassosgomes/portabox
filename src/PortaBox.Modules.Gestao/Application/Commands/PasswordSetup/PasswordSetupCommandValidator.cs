using FluentValidation;
using Microsoft.Extensions.Options;

namespace PortaBox.Modules.Gestao.Application.Commands.PasswordSetup;

public sealed class PasswordSetupCommandValidator : AbstractValidator<PasswordSetupCommand>
{
    private readonly PasswordSetupPolicyOptions _policy;

    public PasswordSetupCommandValidator(IOptions<PasswordSetupPolicyOptions> optionsAccessor)
    {
        _policy = optionsAccessor.Value;

        RuleFor(command => command.RawToken)
            .NotEmpty();

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(GetMinimumLength())
            .Must(ContainLetter)
            .WithMessage("Password must contain at least one letter.")
            .Must(ContainDigit)
            .WithMessage("Password must contain at least one digit.");

        if (_policy.RequireLowercase)
        {
            RuleFor(command => command.Password)
                .Must(password => password.Any(char.IsLower))
                .WithMessage("Password must contain at least one lowercase letter.");
        }

        if (_policy.RequireUppercase)
        {
            RuleFor(command => command.Password)
                .Must(password => password.Any(char.IsUpper))
                .WithMessage("Password must contain at least one uppercase letter.");
        }

        if (_policy.RequireNonAlphanumeric)
        {
            RuleFor(command => command.Password)
                .Must(password => password.Any(character => !char.IsLetterOrDigit(character)))
                .WithMessage("Password must contain at least one non-alphanumeric character.");
        }
    }

    private int GetMinimumLength()
    {
        return Math.Max(10, _policy.RequiredLength);
    }

    private static bool ContainLetter(string password)
    {
        return !string.IsNullOrWhiteSpace(password) && password.Any(char.IsLetter);
    }

    private static bool ContainDigit(string password)
    {
        return !string.IsNullOrWhiteSpace(password) && password.Any(char.IsDigit);
    }
}
