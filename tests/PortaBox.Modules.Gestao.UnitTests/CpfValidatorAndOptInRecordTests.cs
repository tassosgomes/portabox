using PortaBox.Modules.Gestao.Application.Validators;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class CpfValidatorAndOptInRecordTests
{
    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidCpf()
    {
        Assert.True(CpfValidator.IsValid("12345678909"));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForRepeatedDigits()
    {
        Assert.False(CpfValidator.IsValid("11111111111"));
    }

    [Fact]
    public void Normalize_ShouldStripMask()
    {
        Assert.Equal("12345678909", CpfValidator.Normalize("123.456.789-09"));
    }

    [Fact]
    public void Create_ShouldInitializePropertiesAndNormalizeCpf()
    {
        var now = new DateTimeOffset(2026, 4, 18, 14, 0, 0, TimeSpan.Zero);
        var clock = new FixedTimeProvider(now);
        var tenantId = Guid.NewGuid();
        var registeredByUserId = Guid.NewGuid();

        var record = OptInRecord.Create(
            Guid.NewGuid(),
            tenantId,
            new DateOnly(2026, 4, 10),
            "Maioria simples em segunda chamada",
            " Maria da Silva ",
            "123.456.789-09",
            new DateOnly(2026, 4, 11),
            registeredByUserId,
            clock);

        Assert.Equal(tenantId, record.TenantId);
        Assert.Equal(new DateOnly(2026, 4, 10), record.DataAssembleia);
        Assert.Equal("Maioria simples em segunda chamada", record.QuorumDescricao);
        Assert.Equal("Maria da Silva", record.SignatarioNome);
        Assert.Equal("12345678909", record.SignatarioCpf);
        Assert.Equal(new DateOnly(2026, 4, 11), record.DataTermo);
        Assert.Equal(registeredByUserId, record.RegisteredByUserId);
        Assert.Equal(now, record.RegisteredAt);
    }

    [Fact]
    public void Create_ShouldThrow_ForInvalidCpf()
    {
        var exception = Assert.Throws<ArgumentException>(() => OptInRecord.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 4, 10),
            "Unanimidade",
            "Maria da Silva",
            "11111111111",
            new DateOnly(2026, 4, 11),
            Guid.NewGuid(),
            TimeProvider.System));

        Assert.Equal("signatarioCpf", exception.ParamName);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
