using PortaBox.Modules.Gestao.Application.Validators;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class CnpjValidatorAndCondominioTests
{
    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidCnpj()
    {
        Assert.True(CnpjValidator.IsValid("12345678000195"));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForRepeatedDigits()
    {
        Assert.False(CnpjValidator.IsValid("11111111111111"));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForInvalidLength()
    {
        Assert.False(CnpjValidator.IsValid("123"));
    }

    [Fact]
    public void Normalize_ShouldStripMask()
    {
        Assert.Equal("12345678000195", CnpjValidator.Normalize("12.345.678/0001-95"));
    }

    [Fact]
    public void Create_ShouldInitializePreAtivoStatusAndCreatedAtFromInjectedClock()
    {
        var now = new DateTimeOffset(2026, 4, 18, 12, 34, 56, TimeSpan.Zero);
        var clock = new FixedTimeProvider(now);

        var condominio = Condominio.Create(
            Guid.NewGuid(),
            "Residencial Bosque Azul",
            "12.345.678/0001-95",
            Guid.NewGuid(),
            clock);

        Assert.Equal(CondominioStatus.PreAtivo, condominio.Status);
        Assert.Equal(now, condominio.CreatedAt);
        Assert.Equal("12345678000195", condominio.Cnpj);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
