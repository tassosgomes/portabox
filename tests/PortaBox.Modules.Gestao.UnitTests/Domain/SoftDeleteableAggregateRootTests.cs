using PortaBox.Domain;

namespace PortaBox.Modules.Gestao.UnitTests.Domain;

public sealed class SoftDeleteableAggregateRootTests
{
    [Fact]
    public void NewEntity_ShouldStartAsActive()
    {
        var entity = new TestSoftDeleteableAggregate();

        Assert.True(entity.Ativo);
        Assert.Null(entity.InativadoEm);
        Assert.Null(entity.InativadoPor);
    }

    [Fact]
    public void Inativar_WhenEntityIsActive_ShouldUpdateStateAndReturnSuccess()
    {
        var entity = new TestSoftDeleteableAggregate();
        var userId = Guid.NewGuid();
        var now = new DateTime(2026, 4, 20, 10, 30, 0, DateTimeKind.Utc);

        var result = entity.InativarPublic(userId, now);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.False(entity.Ativo);
        Assert.Equal(now, entity.InativadoEm);
        Assert.Equal(userId, entity.InativadoPor);
    }

    [Fact]
    public void Inativar_WhenEntityIsAlreadyInactive_ShouldReturnFailureAndPreserveState()
    {
        var entity = new TestSoftDeleteableAggregate();
        var originalUserId = Guid.NewGuid();
        var originalTime = new DateTime(2026, 4, 20, 10, 30, 0, DateTimeKind.Utc);
        entity.InativarPublic(originalUserId, originalTime);

        var result = entity.InativarPublic(Guid.NewGuid(), originalTime.AddMinutes(5));

        Assert.False(result.IsSuccess);
        Assert.Equal("A entidade ja esta inativa.", result.Error);
        Assert.False(entity.Ativo);
        Assert.Equal(originalTime, entity.InativadoEm);
        Assert.Equal(originalUserId, entity.InativadoPor);
    }

    [Fact]
    public void Reativar_WhenEntityIsInactive_ShouldUpdateStateAndReturnSuccess()
    {
        var entity = new TestSoftDeleteableAggregate();
        entity.InativarPublic(Guid.NewGuid(), new DateTime(2026, 4, 20, 10, 30, 0, DateTimeKind.Utc));

        var result = entity.ReativarPublic(Guid.NewGuid(), new DateTime(2026, 4, 20, 11, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.True(entity.Ativo);
        Assert.Null(entity.InativadoEm);
        Assert.Null(entity.InativadoPor);
    }

    [Fact]
    public void Reativar_WhenEntityIsAlreadyActive_ShouldReturnFailureAndPreserveState()
    {
        var entity = new TestSoftDeleteableAggregate();

        var result = entity.ReativarPublic(Guid.NewGuid(), new DateTime(2026, 4, 20, 11, 0, 0, DateTimeKind.Utc));

        Assert.False(result.IsSuccess);
        Assert.Equal("A entidade ja esta ativa.", result.Error);
        Assert.True(entity.Ativo);
        Assert.Null(entity.InativadoEm);
        Assert.Null(entity.InativadoPor);
    }

    private sealed class TestSoftDeleteableAggregate : SoftDeleteableAggregateRoot
    {
        public Result InativarPublic(Guid porUserId, DateTime agoraUtc) => Inativar(porUserId, agoraUtc);

        public Result ReativarPublic(Guid porUserId, DateTime agoraUtc) => Reativar(porUserId, agoraUtc);
    }
}
