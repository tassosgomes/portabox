using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Application.Unidades;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Blocos;
using PortaBox.Modules.Gestao.Domain.Unidades;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class InativarReativarUnidadeCommandHandlerTests
{
    [Fact]
    public async Task Inativar_HappyPath_ShouldTransitionAuditAndSave()
    {
        var unidade = CreateUnidade();
        var repository = new Mock<IUnidadeRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(unidade.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unidade);
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

        var handler = new InativarUnidadeCommandHandler(
            new InativarUnidadeCommandValidator(),
            repository.Object,
            auditService.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 14, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(
            new InativarUnidadeCommand(Guid.NewGuid(), unidade.BlocoId, unidade.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(unidade.Ativo);
        Assert.Equal(TenantAuditEventKind.UnidadeInativada, kind);
        repository.Verify(current => current.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Inativar_AlreadyInactive_ShouldReturnFailure()
    {
        var unidade = CreateUnidade();
        unidade.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        var repository = new Mock<IUnidadeRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(unidade.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unidade);

        var handler = new InativarUnidadeCommandHandler(
            new InativarUnidadeCommandValidator(),
            repository.Object,
            new Mock<IAuditService>().Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(
            new InativarUnidadeCommand(Guid.NewGuid(), unidade.BlocoId, unidade.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("A entidade ja esta inativa.", result.Error);
    }

    [Fact]
    public async Task Reativar_HappyPath_ShouldLoadIncludingInactiveAuditAndSave()
    {
        var unidade = CreateUnidade();
        unidade.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        var repository = new Mock<IUnidadeRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(unidade.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unidade);
        repository
            .Setup(current => current.ExistsActiveWithCanonicalAsync(unidade.TenantId, unidade.BlocoId, unidade.Andar, unidade.Numero, It.IsAny<CancellationToken>()))
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

        var handler = new ReativarUnidadeCommandHandler(
            new ReativarUnidadeCommandValidator(),
            CreateBlocoRepository(unidade),
            repository.Object,
            auditService.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 15, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(
            new ReativarUnidadeCommand(unidade.TenantId, unidade.BlocoId, unidade.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(unidade.Ativo);
        Assert.Equal(TenantAuditEventKind.UnidadeReativada, kind);
        repository.Verify(current => current.GetByIdIncludingInactiveAsync(unidade.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reativar_WhenAnotherActiveUnitHasSameCanonicalForm_ShouldReturnConflictFailure()
    {
        var unidade = CreateUnidade();
        unidade.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        var repository = new Mock<IUnidadeRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(unidade.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unidade);
        repository
            .Setup(current => current.ExistsActiveWithCanonicalAsync(unidade.TenantId, unidade.BlocoId, unidade.Andar, unidade.Numero, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var auditService = new Mock<IAuditService>();
        var handler = new ReativarUnidadeCommandHandler(
            new ReativarUnidadeCommandValidator(),
            CreateBlocoRepository(unidade),
            repository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(
            new ReativarUnidadeCommand(unidade.TenantId, unidade.BlocoId, unidade.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflito canonico; inative a duplicada antes de reativar esta unidade", result.Error);
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
        var unidade = CreateUnidade();
        unidade.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        var repository = new Mock<IUnidadeRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(unidade.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unidade);
        repository
            .Setup(current => current.ExistsActiveWithCanonicalAsync(unidade.TenantId, unidade.BlocoId, unidade.Andar, unidade.Numero, It.IsAny<CancellationToken>()))
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

        var handler = new ReativarUnidadeCommandHandler(
            new ReativarUnidadeCommandValidator(),
            CreateBlocoRepository(unidade),
            repository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(
            new ReativarUnidadeCommand(unidade.TenantId, unidade.BlocoId, unidade.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflito canonico; inative a duplicada antes de reativar esta unidade", result.Error);
    }

    [Fact]
    public async Task Reativar_WhenParentBlockIsInactive_ShouldReturnInvalidTransition()
    {
        var unidade = CreateUnidade();
        unidade.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        var bloco = CreateBlocoForUnidade(unidade);
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 13, 0, 0, DateTimeKind.Utc));

        var repository = new Mock<IUnidadeRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(unidade.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unidade);

        var blocoRepository = new Mock<IBlocoRepository>();
        blocoRepository
            .Setup(current => current.GetByIdIncludingInactiveAsync(unidade.BlocoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);

        var auditService = new Mock<IAuditService>();
        var handler = new ReativarUnidadeCommandHandler(
            new ReativarUnidadeCommandValidator(),
            blocoRepository.Object,
            repository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(
            new ReativarUnidadeCommand(unidade.TenantId, unidade.BlocoId, unidade.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Nao e possivel reativar unidade em bloco inativo.", result.Error);
        repository.Verify(current => current.ExistsActiveWithCanonicalAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        auditService.Verify(current => current.RecordStructuralAsync(
            It.IsAny<TenantAuditEventKind>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Unidade CreateUnidade()
    {
        var bloco = Bloco.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Bloco A",
            Guid.NewGuid(),
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero))).Value!;

        return Unidade.Create(
            Guid.NewGuid(),
            bloco.TenantId,
            bloco,
            2,
            "201",
            Guid.NewGuid(),
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 12, 5, 0, TimeSpan.Zero))).Value!;
    }

    private static IBlocoRepository CreateBlocoRepository(Unidade unidade)
    {
        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdIncludingInactiveAsync(unidade.BlocoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateBlocoForUnidade(unidade));
        return repository.Object;
    }

    private static Bloco CreateBlocoForUnidade(Unidade unidade)
    {
        return Bloco.Create(
            unidade.BlocoId,
            unidade.TenantId,
            unidade.TenantId,
            "Bloco A",
            Guid.NewGuid(),
            TimeProvider.System).Value!;
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
