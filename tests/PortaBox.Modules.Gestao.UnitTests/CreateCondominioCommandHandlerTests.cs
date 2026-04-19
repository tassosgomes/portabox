using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PortaBox.Application.Abstractions.Identity;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class CreateCondominioCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_DuplicateCnpj_ShouldReturnFailure()
    {
        var validator = new CreateCondominioCommandValidator();
        var condominioRepository = new Mock<ICondominioRepository>();
        condominioRepository
            .Setup(repository => repository.ExistsByCnpjAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = BuildHandler(
            validator,
            condominioRepository,
            new Mock<ISindicoRepository>(),
            new Mock<IOptInRecordRepository>(),
            new Mock<ITenantAuditRepository>(),
            new Mock<IIdentityUserProvisioningService>(),
            new Mock<IApplicationDbSession>());

        var result = await handler.HandleAsync(BuildCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateCondominioErrors.CnpjAlreadyExists, result.Error);
    }

    [Fact]
    public async Task HandleAsync_DuplicateSindicoEmail_ShouldReturnFailure()
    {
        var validator = new CreateCondominioCommandValidator();
        var condominioRepository = new Mock<ICondominioRepository>();
        condominioRepository
            .Setup(repository => repository.ExistsByCnpjAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var identityService = new Mock<IIdentityUserProvisioningService>();
        identityService
            .Setup(service => service.CreateSindicoUserAsync(It.IsAny<CreateSindicoUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSindicoUserResult.Failure(CreateCondominioErrors.SindicoEmailAlreadyExists));

        var dbSession = BuildDbSession();
        var handler = BuildHandler(
            validator,
            condominioRepository,
            new Mock<ISindicoRepository>(),
            new Mock<IOptInRecordRepository>(),
            new Mock<ITenantAuditRepository>(),
            identityService,
            dbSession);

        var result = await handler.HandleAsync(BuildCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateCondominioErrors.SindicoEmailAlreadyExists, result.Error);
    }

    [Fact]
    public async Task HandleAsync_HappyPath_ShouldCreateAggregateGraphAndRaiseEvent()
    {
        var validator = new CreateCondominioCommandValidator();
        var condominioRepository = new Mock<ICondominioRepository>();
        condominioRepository
            .Setup(repository => repository.ExistsByCnpjAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Condominio? persistedCondominio = null;
        Sindico? persistedSindico = null;
        OptInRecord? persistedOptInRecord = null;
        TenantAuditEntry? persistedAudit = null;

        condominioRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Condominio>(), It.IsAny<CancellationToken>()))
            .Callback<Condominio, CancellationToken>((condominio, _) => persistedCondominio = condominio)
            .Returns(Task.CompletedTask);

        var sindicoRepository = new Mock<ISindicoRepository>();
        sindicoRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Sindico>(), It.IsAny<CancellationToken>()))
            .Callback<Sindico, CancellationToken>((sindico, _) => persistedSindico = sindico)
            .Returns(Task.CompletedTask);

        var optInRepository = new Mock<IOptInRecordRepository>();
        optInRepository
            .Setup(repository => repository.AddAsync(It.IsAny<OptInRecord>(), It.IsAny<CancellationToken>()))
            .Callback<OptInRecord, CancellationToken>((record, _) => persistedOptInRecord = record)
            .Returns(Task.CompletedTask);

        var auditRepository = new Mock<ITenantAuditRepository>();
        auditRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TenantAuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TenantAuditEntry, CancellationToken>((entry, _) => persistedAudit = entry)
            .Returns(Task.CompletedTask);

        var identityService = new Mock<IIdentityUserProvisioningService>();
        var sindicoUserId = Guid.NewGuid();
        identityService
            .Setup(service => service.CreateSindicoUserAsync(It.IsAny<CreateSindicoUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSindicoUserResult.Success(new IdentityUserDescriptor(
                sindicoUserId,
                "sindico@portabox.test",
                "Joao da Silva",
                null)));

        var dbSession = BuildDbSession();
        var handler = BuildHandler(
            validator,
            condominioRepository,
            sindicoRepository,
            optInRepository,
            auditRepository,
            identityService,
            dbSession,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 18, 18, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(BuildCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(persistedCondominio);
        Assert.NotNull(persistedSindico);
        Assert.NotNull(persistedOptInRecord);
        Assert.NotNull(persistedAudit);
        Assert.Equal(CondominioStatus.PreAtivo, persistedCondominio!.Status);
        Assert.Equal(result.Value!.CondominioId, persistedCondominio.Id);
        Assert.Equal(result.Value.SindicoUserId, persistedSindico!.UserId);
        Assert.Equal(result.Value.CondominioId, persistedSindico.TenantId);
        Assert.Equal(result.Value.CondominioId, persistedOptInRecord!.TenantId);
        Assert.Equal(TenantAuditEventKind.Created, persistedAudit!.EventKind);
        Assert.Single(persistedCondominio.DomainEvents);
        Assert.Equal("condominio.cadastrado.v1", persistedCondominio.DomainEvents[0].EventType);
    }

    private static CreateCondominioCommandHandler BuildHandler(
        IValidator<CreateCondominioCommand> validator,
        Mock<ICondominioRepository> condominioRepository,
        Mock<ISindicoRepository> sindicoRepository,
        Mock<IOptInRecordRepository> optInRecordRepository,
        Mock<ITenantAuditRepository> tenantAuditRepository,
        Mock<IIdentityUserProvisioningService> identityUserProvisioningService,
        Mock<IApplicationDbSession> dbSession,
        TimeProvider? timeProvider = null)
    {
        return new CreateCondominioCommandHandler(
            validator,
            condominioRepository.Object,
            sindicoRepository.Object,
            optInRecordRepository.Object,
            tenantAuditRepository.Object,
            identityUserProvisioningService.Object,
            dbSession.Object,
            new Mock<IGestaoMetrics>().Object,
            NullLogger<CreateCondominioCommandHandler>.Instance,
            timeProvider ?? TimeProvider.System);
    }

    private static Mock<IApplicationDbSession> BuildDbSession()
    {
        var dbSession = new Mock<IApplicationDbSession>();
        dbSession
            .Setup(session => session.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return dbSession;
    }

    private static CreateCondominioCommand BuildCommand()
    {
        return new CreateCondominioCommand(
            Guid.NewGuid(),
            "Residencial Bosque Azul",
            "12.345.678/0001-95",
            "Rua das Palmeiras",
            "123",
            null,
            "Centro",
            "Fortaleza",
            "CE",
            "60000000",
            "Admin XPTO",
            new DateOnly(2026, 4, 10),
            "Maioria simples",
            "Maria da Silva",
            "123.456.789-09",
            new DateOnly(2026, 4, 11),
            "Joao da Silva",
            "sindico@portabox.test",
            "+5585999990001");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
