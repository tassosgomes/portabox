using FluentValidation.TestHelper;
using PortaBox.Modules.Gestao.Application.Commands.ActivateCondominio;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class ActivateCondominioCommandValidatorTests
{
    private readonly ActivateCondominioCommandValidator _validator = new();

    [Fact]
    public void Validate_NoteLongerThan500Characters_ShouldReject()
    {
        var command = new ActivateCondominioCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 501));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(current => current.Note);
    }
}
