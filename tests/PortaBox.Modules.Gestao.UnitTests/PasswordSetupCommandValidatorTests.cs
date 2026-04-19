using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using PortaBox.Modules.Gestao.Application.Commands.PasswordSetup;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class PasswordSetupCommandValidatorTests
{
    private readonly PasswordSetupCommandValidator _validator = new(Options.Create(new PasswordSetupPolicyOptions()));

    [Fact]
    public void Validate_PasswordShorterThanTenCharacters_ShouldReject()
    {
        var command = new PasswordSetupCommand("token", "Abc12345", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(current => current.Password);
    }

    [Fact]
    public void Validate_PasswordWithoutDigit_ShouldReject()
    {
        var command = new PasswordSetupCommand("token", "abcdefghij", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(current => current.Password);
    }
}
