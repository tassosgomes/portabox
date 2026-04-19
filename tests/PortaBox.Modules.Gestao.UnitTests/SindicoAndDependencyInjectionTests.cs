using Microsoft.Extensions.DependencyInjection;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class SindicoAndDependencyInjectionTests
{
    [Fact]
    public void Create_ShouldInitializePropertiesAndCreatedAtFromInjectedClock()
    {
        var now = new DateTimeOffset(2026, 4, 18, 15, 0, 0, TimeSpan.Zero);
        var clock = new FixedTimeProvider(now);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var sindico = Sindico.Create(
            Guid.NewGuid(),
            tenantId,
            userId,
            " Maria Oliveira ",
            " +5585999990001 ",
            clock);

        Assert.Equal(tenantId, sindico.TenantId);
        Assert.Equal(userId, sindico.UserId);
        Assert.Equal("Maria Oliveira", sindico.NomeCompleto);
        Assert.Equal("+5585999990001", sindico.CelularE164);
        Assert.Equal(SindicoStatus.Ativo, sindico.Status);
        Assert.Equal(now, sindico.CreatedAt);
    }

    [Fact]
    public void AddPortaBoxModuleGestao_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returnedServices = PortaBox.Modules.Gestao.DependencyInjection.AddPortaBoxModuleGestao(services);

        Assert.Same(services, returnedServices);
    }

    [Fact]
    public void AddPortaBoxModuleGestao_ShouldThrowWhenServicesIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            PortaBox.Modules.Gestao.DependencyInjection.AddPortaBoxModuleGestao(null!));

        Assert.Equal("services", exception.ParamName);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
