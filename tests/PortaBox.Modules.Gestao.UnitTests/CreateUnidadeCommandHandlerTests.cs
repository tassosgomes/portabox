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

public sealed class CreateUnidadeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_HappyPath_ShouldAddAuditSaveAndReturnDto()
    {
        var bloco = CreateBloco();
        Unidade? persistedUnidade = null;
        string? checkedNumero = null;

        var blocoRepository = new Mock<IBlocoRepository>();
        blocoRepository
            .Setup(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);

        var unidadeRepository = new Mock<IUnidadeRepository>();
        unidadeRepository
            .Setup(current => current.ExistsActiveWithCanonicalAsync(bloco.TenantId, bloco.Id, 10, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, int, string, CancellationToken>((_, _, _, numero, _) => checkedNumero = numero)
            .ReturnsAsync(false);
        unidadeRepository
            .Setup(current => current.AddAsync(It.IsAny<Unidade>(), It.IsAny<CancellationToken>()))
            .Callback<Unidade, CancellationToken>((unidade, _) => persistedUnidade = unidade)
            .Returns(Task.CompletedTask);
        unidadeRepository
            .Setup(current => current.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        TenantAuditEventKind? recordedKind = null;
        IDictionary<string, object>? recordedMetadata = null;
        var auditService = new Mock<IAuditService>();
        auditService
            .Setup(current => current.RecordStructuralAsync(
                It.IsAny<TenantAuditEventKind>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<TenantAuditEventKind, Guid, Guid, IDictionary<string, object>, string?, CancellationToken>((kind, _, _, metadata, _, _) =>
            {
                recordedKind = kind;
                recordedMetadata = metadata;
            })
            .Returns(Task.CompletedTask);

        var handler = new CreateUnidadeCommandHandler(
            new CreateUnidadeCommandValidator(),
            blocoRepository.Object,
            unidadeRepository.Object,
            auditService.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(
            new CreateUnidadeCommand(bloco.CondominioId, bloco.Id, 10, " 101a ", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(persistedUnidade);
        Assert.Equal("101A", checkedNumero);
        Assert.Equal("101A", persistedUnidade!.Numero);
        Assert.Equal(TenantAuditEventKind.UnidadeCriada, recordedKind);
        Assert.NotNull(recordedMetadata);
        Assert.Equal(persistedUnidade.Id, result.Value!.Id);
        Assert.Equal("101A", result.Value.Numero);
        Assert.Equal(persistedUnidade.Id, recordedMetadata!["unidadeId"]);
        unidadeRepository.Verify(current => current.AddAsync(It.IsAny<Unidade>(), It.IsAny<CancellationToken>()), Times.Once);
        unidadeRepository.Verify(current => current.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenBlocoDoesNotExist_ShouldReturnFailure()
    {
        var blocoRepository = new Mock<IBlocoRepository>();
        blocoRepository
            .Setup(current => current.GetByIdIncludingInactiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bloco?)null);

        var unidadeRepository = new Mock<IUnidadeRepository>();
        var auditService = new Mock<IAuditService>();
        var handler = new CreateUnidadeCommandHandler(
            new CreateUnidadeCommandValidator(),
            blocoRepository.Object,
            unidadeRepository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(
            new CreateUnidadeCommand(Guid.NewGuid(), Guid.NewGuid(), 1, "101", Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Bloco nao encontrado", result.Error);
        unidadeRepository.Verify(current => current.AddAsync(It.IsAny<Unidade>(), It.IsAny<CancellationToken>()), Times.Never);
        auditService.Verify(current => current.RecordStructuralAsync(
            It.IsAny<TenantAuditEventKind>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenBlocoIsInactive_ShouldReturnFailure()
    {
        var bloco = CreateBloco();
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 13, 0, 0, DateTimeKind.Utc));

        var blocoRepository = new Mock<IBlocoRepository>();
        blocoRepository
            .Setup(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);

        var unidadeRepository = new Mock<IUnidadeRepository>();
        var auditService = new Mock<IAuditService>();
        var handler = new CreateUnidadeCommandHandler(
            new CreateUnidadeCommandValidator(),
            blocoRepository.Object,
            unidadeRepository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(
            new CreateUnidadeCommand(bloco.CondominioId, bloco.Id, 1, "101", Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Bloco inativo", result.Error);
        unidadeRepository.Verify(current => current.AddAsync(It.IsAny<Unidade>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCanonicalAlreadyExists_ShouldReturnFailureAndNotAdd()
    {
        var bloco = CreateBloco();

        var blocoRepository = new Mock<IBlocoRepository>();
        blocoRepository
            .Setup(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);

        var unidadeRepository = new Mock<IUnidadeRepository>();
        unidadeRepository
            .Setup(current => current.ExistsActiveWithCanonicalAsync(bloco.TenantId, bloco.Id, 1, "101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var auditService = new Mock<IAuditService>();
        var handler = new CreateUnidadeCommandHandler(
            new CreateUnidadeCommandValidator(),
            blocoRepository.Object,
            unidadeRepository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(
            new CreateUnidadeCommand(bloco.CondominioId, bloco.Id, 1, "101", Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unidade ja existe", result.Error);
        unidadeRepository.Verify(current => current.AddAsync(It.IsAny<Unidade>(), It.IsAny<CancellationToken>()), Times.Never);
        auditService.Verify(current => current.RecordStructuralAsync(
            It.IsAny<TenantAuditEventKind>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DbUpdateUniqueViolation_ShouldReturnFailure()
    {
        var bloco = CreateBloco();

        var blocoRepository = new Mock<IBlocoRepository>();
        blocoRepository
            .Setup(current => current.GetByIdIncludingInactiveAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);

        var unidadeRepository = new Mock<IUnidadeRepository>();
        unidadeRepository
            .Setup(current => current.ExistsActiveWithCanonicalAsync(bloco.TenantId, bloco.Id, 1, "101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        unidadeRepository
            .Setup(current => current.AddAsync(It.IsAny<Unidade>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unidadeRepository
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

        var handler = new CreateUnidadeCommandHandler(
            new CreateUnidadeCommandValidator(),
            blocoRepository.Object,
            unidadeRepository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(
            new CreateUnidadeCommand(bloco.CondominioId, bloco.Id, 1, "101", Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unidade ja existe", result.Error);
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
