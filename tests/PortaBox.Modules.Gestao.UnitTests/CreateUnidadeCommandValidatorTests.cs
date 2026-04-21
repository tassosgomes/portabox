using PortaBox.Modules.Gestao.Application.Unidades;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class CreateUnidadeCommandValidatorTests
{
    [Theory]
    [InlineData(-1, "101", false)]
    [InlineData(0, "1AB", false)]
    [InlineData(0, " ", false)]
    [InlineData(0, "12345", false)]
    [InlineData(0, "101a", true)]
    public void Validate_ShouldRespectCanonicalRules(int andar, string numero, bool expectedIsValid)
    {
        var validator = new CreateUnidadeCommandValidator();

        var result = validator.Validate(new CreateUnidadeCommand(Guid.NewGuid(), Guid.NewGuid(), andar, numero, Guid.NewGuid()));

        Assert.Equal(expectedIsValid, result.IsValid);
    }
}
