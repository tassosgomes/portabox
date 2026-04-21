using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Blocos;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class CreateBlocoCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_HappyPath_ShouldAddAuditSaveAndReturnDto()
    {
        Bloco? persistedBloco = null;
        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.ExistsActiveWithNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(current => current.AddAsync(It.IsAny<Bloco>(), It.IsAny<CancellationToken>()))
            .Callback<Bloco, CancellationToken>((bloco, _) => persistedBloco = bloco)
            .Returns(Task.CompletedTask);
        repository
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

        var handler = new CreateBlocoCommandHandler(
            new CreateBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(new CreateBlocoCommand(Guid.NewGuid(), "  Bloco A  ", Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(persistedBloco);
        Assert.Equal(TenantAuditEventKind.BlocoCriado, recordedKind);
        Assert.NotNull(recordedMetadata);
        Assert.Equal(persistedBloco!.Id, result.Value!.Id);
        Assert.Equal("Bloco A", result.Value.Nome);
        Assert.Equal(persistedBloco.Id, recordedMetadata!["blocoId"]);
        repository.Verify(current => current.AddAsync(It.IsAny<Bloco>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(current => current.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DuplicateActiveName_ShouldReturnFailureAndNotAdd()
    {
        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.ExistsActiveWithNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var auditService = new Mock<IAuditService>();
        var handler = new CreateBlocoCommandHandler(
            new CreateBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(new CreateBlocoCommand(Guid.NewGuid(), "Bloco A", Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ja existe bloco ativo com este nome", result.Error);
        repository.Verify(current => current.AddAsync(It.IsAny<Bloco>(), It.IsAny<CancellationToken>()), Times.Never);
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
        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.ExistsActiveWithNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(current => current.AddAsync(It.IsAny<Bloco>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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

        var handler = new CreateBlocoCommandHandler(
            new CreateBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(new CreateBlocoCommand(Guid.NewGuid(), "Bloco A", Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ja existe bloco ativo com este nome", result.Error);
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
