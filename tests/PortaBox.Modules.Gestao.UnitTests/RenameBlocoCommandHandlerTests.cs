using Moq;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Blocos;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class RenameBlocoCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_HappyPath_ShouldRenameAuditAndSave()
    {
        var bloco = CreateBloco();
        IDictionary<string, object>? metadata = null;
        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);
        repository
            .Setup(current => current.ExistsActiveWithNameAsync(bloco.CondominioId, "Torre Alfa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(current => current.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var auditService = new Mock<IAuditService>();
        auditService
            .Setup(current => current.RecordStructuralAsync(
                TenantAuditEventKind.BlocoRenomeado,
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<TenantAuditEventKind, Guid, Guid, IDictionary<string, object>, string?, CancellationToken>((_, _, _, currentMetadata, _, _) => metadata = currentMetadata)
            .Returns(Task.CompletedTask);

        var handler = new RenameBlocoCommandHandler(
            new RenameBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 13, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(new RenameBlocoCommand(bloco.CondominioId, bloco.Id, "Torre Alfa", Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Torre Alfa", bloco.Nome);
        Assert.NotNull(metadata);
        Assert.Equal("Bloco A", metadata!["nomeAntes"]);
        Assert.Equal("Torre Alfa", metadata["nomeDepois"]);
        repository.Verify(current => current.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UnknownBloco_ShouldReturnFailure()
    {
        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bloco?)null);

        var handler = new RenameBlocoCommandHandler(
            new RenameBlocoCommandValidator(),
            repository.Object,
            new Mock<IAuditService>().Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(new RenameBlocoCommand(Guid.NewGuid(), Guid.NewGuid(), "Torre Alfa", Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Bloco nao encontrado", result.Error);
    }

    [Fact]
    public async Task HandleAsync_InactiveBloco_ShouldPropagateDomainFailure()
    {
        var bloco = CreateBloco();
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 12, 30, 0, DateTimeKind.Utc));

        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);
        repository
            .Setup(current => current.ExistsActiveWithNameAsync(bloco.CondominioId, "Torre Alfa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var auditService = new Mock<IAuditService>();
        var handler = new RenameBlocoCommandHandler(
            new RenameBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(new RenameBlocoCommand(bloco.CondominioId, bloco.Id, "Torre Alfa", Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Nao e possivel renomear bloco inativo.", result.Error);
        auditService.Verify(current => current.RecordStructuralAsync(
            It.IsAny<TenantAuditEventKind>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SameName_ShouldReturnFailureWithoutAuditOrSave()
    {
        var bloco = CreateBloco();
        var repository = new Mock<IBlocoRepository>();
        repository
            .Setup(current => current.GetByIdAsync(bloco.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bloco);

        var auditService = new Mock<IAuditService>();
        var handler = new RenameBlocoCommandHandler(
            new RenameBlocoCommandValidator(),
            repository.Object,
            auditService.Object,
            TimeProvider.System);

        var result = await handler.HandleAsync(new RenameBlocoCommand(bloco.CondominioId, bloco.Id, bloco.Nome, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("O novo nome do bloco deve ser diferente do nome atual.", result.Error);
        auditService.Verify(current => current.RecordStructuralAsync(
            It.IsAny<TenantAuditEventKind>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(current => current.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
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

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
