using Moq;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Modules.Gestao.Application.Common;
using PortaBox.Modules.Gestao.Application.Queries.ListCondominios;
using PortaBox.Modules.Gestao.Application.Repositories;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class ListCondominiosQueryAndMaskingTests
{
    [Fact]
    public void Constructor_PageSizeAboveMaximum_ShouldClampTo100()
    {
        var query = new ListCondominiosQuery(pageSize: 150);

        Assert.Equal(100, query.PageSize);
    }

    [Fact]
    public void Cnpj_ValidDigits_ShouldMaskKeepingLastSevenDigits()
    {
        var masked = Masking.Cnpj("12345678000195");

        Assert.Equal("****8000195", masked);
    }

    [Fact]
    public void Cpf_ValidDigits_ShouldMaskMiddleDigitsOnly()
    {
        var masked = Masking.Cpf("12345678909");

        Assert.Equal("***.456.789-**", masked);
    }

    [Fact]
    public void Celular_E164_ShouldMaskSubscriberNumber()
    {
        var masked = Masking.Celular("+5511999998888");

        Assert.Equal("+55 11 9****-8888", masked);
    }

    [Fact]
    public async Task HandleAsync_WithOperatorContext_ShouldPassNullTenantToRepository()
    {
        var query = new ListCondominiosQuery();
        var repository = new Mock<ICondominioRepository>();
        repository
            .Setup(current => current.ListAsync(query, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortaBox.Modules.Gestao.Application.Common.PagedResult<CondominioListItemDto>([], 1, 20, 0));

        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(current => current.TenantId).Returns((Guid?)null);

        var result = await new ListCondominiosQueryHandler(repository.Object, tenantContext.Object)
            .HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.Verify(current => current.ListAsync(query, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
