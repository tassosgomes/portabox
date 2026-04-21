using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Blocos;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class InativarReativarBlocoCommandHandlerTests
{
    [Fact]
    public async Task Inativar_HappyPath_ShouldTransitionAuditAndSave()
    {
        var bloco = CreateBloco();
        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);
        repository
            .Setup(current => current.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        TenantAuditEventKind? kind = null;
        var auditService = new Mock<IAuditService>();
        auditService
            .Setup(current => current.RecordStructuralAsync(
                It.IsAny<TenantAuditEventKind>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<TenantAuditEventKind, Guid, Guid, IDictionary<string, object>, string?, CancellationToken>((currentKind, _, _, _, _, _) => kind = currentKind)
            .Returns(Task.CompletedTask);

        var handler = new InativarBlocoCommandHandler(
            new InativarBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 14, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(new InativarBlocoCommand(bloco.CondominioId, bloco.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(bloco.Ativo);
        Assert.Equal(TenantAuditEventKind.BlocoInativado, kind);
        repository.Verify(current => current.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Inativar_AlreadyInactive_ShouldReturnFailure()
    {
        var bloco = CreateBloco();
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);

        var handler = new InativarBlocoCommandHandler(
            new InativarBlocoCommandValidator(),
            repository.Object,
            new Mock<IAuditService>().Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(new InativarBlocoCommand(bloco.CondominioId, bloco.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("A entidade ja esta inativa.", result.Error);
    }

    [Fact]
    public async Task Reativar_HappyPath_ShouldLoadIncludingInactiveAuditAndSave()
    {
        var bloco = CreateBloco();
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);
        repository
            .Setup(current => current.ExistsActiveWithNameAsync(bloco.CondominioId, bloco.Nome, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(current => current.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        TenantAuditEventKind? kind = null;
        var auditService = new Mock<IAuditService>();
        auditService
            .Setup(current => current.RecordStructuralAsync(
                It.IsAny<TenantAuditEventKind>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<TenantAuditEventKind, Guid, Guid, IDictionary<string, object>, string?, CancellationToken>((currentKind, _, _, _, _, _) => kind = currentKind)
            .Returns(Task.CompletedTask);

        var handler = new ReativarBlocoCommandHandler(
            new ReativarBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 15, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(new ReativarBlocoCommand(bloco.CondominioId, bloco.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(bloco.Ativo);
        Assert.Equal(TenantAuditEventKind.BlocoReativado, kind);
        repository.Verify(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reativar_WhenAnotherActiveBlocoHasSameName_ShouldReturnConflictFailure()
    {
        var bloco = CreateBloco();
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);
        repository
            .Setup(current => current.ExistsActiveWithNameAsync(bloco.CondominioId, bloco.Nome, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var auditService = new Mock<IAuditService>();
        var handler = new ReativarBlocoCommandHandler(
            new ReativarBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(new ReativarBlocoCommand(bloco.CondominioId, bloco.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ja existe bloco ativo com este nome; conflito canonico, inative o outro antes", result.Error);
        repository.Verify(current => current.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        auditService.Verify(current => current.RecordStructuralAsync(
            It.IsAny<TenantAuditEventKind>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reativar_WhenAlreadyActive_ShouldReturnInvalidTransitionInsteadOfConflict()
    {
        var bloco = CreateBloco();

        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);

        var auditService = new Mock<IAuditService>();
        var handler = new ReativarBlocoCommandHandler(
            new ReativarBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(new ReativarBlocoCommand(bloco.CondominioId, bloco.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("A entidade ja esta ativa.", result.Error);
        repository.Verify(current => current.ExistsActiveWithNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(current => current.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        auditService.Verify(current => current.RecordStructuralAsync(
            It.IsAny<TenantAuditEventKind>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reativar_DbUpdateUniqueViolation_ShouldReturnFailure()
    {
        var bloco = CreateBloco();
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);
        repository
            .Setup(current => current.ExistsActiveWithNameAsync(bloco.CondominioId, bloco.Nome, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(current => current.SaveAsync(It.IsAny<CancellationToken>()))
            .Throws(CreateUniqueViolationException());

        var auditService = new Mock<IAuditService>();
        auditService
            .Setup(current => current.RecordStructuralAsync(
                It.IsAny<TenantAuditEventKind>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ReativarBlocoCommandHandler(
            new ReativarBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(new ReativarBlocoCommand(bloco.CondominioId, bloco.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ja existe bloco ativo com este nome", result.Error);
    }

    private static Bloco CreateBloco()
    {
        return Bloco.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Bloco A",
            Guid.NewGuid(),
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero))).Value!;
    }

    private static DbUpdateException CreateUniqueViolationException()
    {
        return new DbUpdateException("duplicate", new PostgresException(
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation,
            "duplicate key value violates unique constraint"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
