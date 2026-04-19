using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Modules.Gestao.Application.Commands.ActivateCondominio;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Events;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class ActivateCondominioCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_UnknownCondominio_ShouldReturnFailure()
    {
        var repository = new Mock<ICondominioRepository>();
        repository
            .Setup(current => current.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Condominio?)null);

        var result = await BuildHandler(condominioRepository: repository).HandleAsync(BuildCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ActivateCondominioErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_AlreadyActive_ShouldReturnFailure()
    {
        var now = new DateTimeOffset(2026, 4, 18, 20, 0, 0, TimeSpan.Zero);
        var command = BuildCommand();
        var condominio = CreateCondominio(command.CondominioId, now, active: true, activatedBy: Guid.NewGuid());
        var repository = new Mock<ICondominioRepository>();
        repository
            .Setup(current => current.GetByIdAsync(command.CondominioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condominio);

        var auditRepository = new Mock<ITenantAuditRepository>();
        var result = await BuildHandler(
            condominioRepository: repository,
            tenantAuditRepository: auditRepository,
            timeProvider: new FixedTimeProvider(now)).HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ActivateCondominioErrors.AlreadyActive, result.Error);
        auditRepository.Verify(current => current.AddAsync(It.IsAny<TenantAuditEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_HappyPath_ShouldActivatePersistAuditAndRaiseEvent()
    {
        var now = new DateTimeOffset(2026, 4, 18, 21, 30, 0, TimeSpan.Zero);
        var command = BuildCommand(note: "  liberar operacao  ");
        var condominio = CreateCondominio(command.CondominioId, now);
        var repository = new Mock<ICondominioRepository>();
        repository
            .Setup(current => current.GetByIdAsync(command.CondominioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condominio);
        repository
            .Setup(current => current.UpdateAsync(condominio, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        TenantAuditEntry? persistedAudit = null;
        var auditRepository = new Mock<ITenantAuditRepository>();
        auditRepository
            .Setup(current => current.AddAsync(It.IsAny<TenantAuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TenantAuditEntry, CancellationToken>((entry, _) => persistedAudit = entry)
            .Returns(Task.CompletedTask);

        var result = await BuildHandler(
            condominioRepository: repository,
            tenantAuditRepository: auditRepository,
            timeProvider: new FixedTimeProvider(now)).HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(CondominioStatus.Ativo, condominio.Status);
        Assert.Equal(now, condominio.ActivatedAt);
        Assert.Equal(command.PerformedByUserId, condominio.ActivatedByUserId);
        Assert.NotNull(persistedAudit);
        Assert.Equal(TenantAuditEventKind.Activated, persistedAudit!.EventKind);
        Assert.Equal("liberar operacao", persistedAudit.Note);
        Assert.Single(condominio.DomainEvents);
        var domainEvent = Assert.IsType<CondominioAtivadoV1>(condominio.DomainEvents[0]);
        Assert.Equal(command.CondominioId, domainEvent.CondominioId);
        Assert.Equal(command.PerformedByUserId, domainEvent.ActivatedByUserId);
    }

    private static ActivateCondominioCommandHandler BuildHandler(
        Mock<ICondominioRepository>? condominioRepository = null,
        Mock<ITenantAuditRepository>? tenantAuditRepository = null,
        Mock<IApplicationDbSession>? dbSession = null,
        TimeProvider? timeProvider = null)
    {
        var session = dbSession ?? new Mock<IApplicationDbSession>();
        session
            .Setup(current => current.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new ActivateCondominioCommandHandler(
            new ActivateCondominioCommandValidator(),
            (condominioRepository ?? new Mock<ICondominioRepository>()).Object,
            (tenantAuditRepository ?? new Mock<ITenantAuditRepository>()).Object,
            session.Object,
            new Mock<IGestaoMetrics>().Object,
            NullLogger<ActivateCondominioCommandHandler>.Instance,
            timeProvider ?? TimeProvider.System);
    }

    private static ActivateCondominioCommand BuildCommand(string? note = "observacao")
    {
        return new ActivateCondominioCommand(Guid.NewGuid(), Guid.NewGuid(), note);
    }

    private static Condominio CreateCondominio(Guid condominioId, DateTimeOffset now, bool active = false, Guid? activatedBy = null)
    {
        var condominio = Condominio.Create(
            condominioId,
            "Residencial Bosque Azul",
            "12.345.678/0001-95",
            Guid.NewGuid(),
            new FixedTimeProvider(now.AddHours(-1)));

        if (active)
        {
            var activated = condominio.TryActivate(activatedBy ?? Guid.NewGuid(), new FixedTimeProvider(now), out _);
            Assert.True(activated);
        }

        return condominio;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
