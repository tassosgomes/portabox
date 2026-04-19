using Moq;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Modules.Gestao.Application.Queries.GetCondominioDetails;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class GetCondominioDetailsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_SindicoFromAnotherTenant_ShouldReturnNotFound()
    {
        var condominioId = Guid.NewGuid();
        var repository = new Mock<ICondominioRepository>();
        repository
            .Setup(current => current.GetDetailsAsync(condominioId, false, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CondominioDetailsDto?)null);

        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(current => current.TenantId).Returns(Guid.NewGuid());

        var result = await new GetCondominioDetailsQueryHandler(repository.Object, tenantContext.Object)
            .HandleAsync(new GetCondominioDetailsQuery(condominioId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("gestao.condominio.not_found", result.Error);
    }

    [Fact]
    public async Task HandleAsync_OperatorContext_ShouldIgnoreTenantFilter()
    {
        var condominioId = Guid.NewGuid();
        var details = new CondominioDetailsDto(
            condominioId,
            "Residencial Bosque Azul",
            "****8000195",
            CondominioStatus.PreAtivo,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [],
            [],
            false);

        var repository = new Mock<ICondominioRepository>();
        repository
            .Setup(current => current.GetDetailsAsync(condominioId, true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(current => current.TenantId).Returns((Guid?)null);

        var result = await new GetCondominioDetailsQueryHandler(repository.Object, tenantContext.Object)
            .HandleAsync(new GetCondominioDetailsQuery(condominioId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.Verify(current => current.GetDetailsAsync(condominioId, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
