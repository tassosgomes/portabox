using FluentValidation.TestHelper;
using PortaBox.Modules.Gestao.Application.Blocos;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class BlocoCommandValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789012345678901234567890123456789012345678901")]
    public void CreateValidator_ShouldRejectInvalidNames(string nome)
    {
        var validator = new CreateBlocoCommandValidator();
        var result = validator.TestValidate(new CreateBlocoCommand(Guid.NewGuid(), nome, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(current => current.Nome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789012345678901234567890123456789012345678901")]
    public void RenameValidator_ShouldRejectInvalidNames(string nome)
    {
        var validator = new RenameBlocoCommandValidator();
        var result = validator.TestValidate(new RenameBlocoCommand(Guid.NewGuid(), Guid.NewGuid(), nome, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(current => current.Nome);
    }
}
