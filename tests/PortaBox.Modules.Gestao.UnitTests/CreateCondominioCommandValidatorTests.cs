using FluentValidation.TestHelper;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class CreateCondominioCommandValidatorTests
{
    private readonly CreateCondominioCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldRejectInvalidCnpjCpfEmailAndE164()
    {
        var command = BuildCommand() with
        {
            Cnpj = "11.111.111/1111-11",
            SignatarioCpf = "111.111.111-11",
            SindicoEmail = "email-invalido",
            SindicoCelularE164 = "85999990001"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Cnpj);
        result.ShouldHaveValidationErrorFor(c => c.SignatarioCpf);
        result.ShouldHaveValidationErrorFor(c => c.SindicoEmail);
        result.ShouldHaveValidationErrorFor(c => c.SindicoCelularE164);
    }

    private static CreateCondominioCommand BuildCommand()
    {
        return new CreateCondominioCommand(
            Guid.NewGuid(),
            "Residencial Bosque Azul",
            "12.345.678/0001-95",
            "Rua das Palmeiras",
            "123",
            null,
            "Centro",
            "Fortaleza",
            "CE",
            "60000000",
            "Admin XPTO",
            new DateOnly(2026, 4, 10),
            "Maioria simples",
            "Maria da Silva",
            "123.456.789-09",
            new DateOnly(2026, 4, 11),
            "Joao da Silva",
            "sindico@portabox.test",
            "+5585999990001");
    }
}
