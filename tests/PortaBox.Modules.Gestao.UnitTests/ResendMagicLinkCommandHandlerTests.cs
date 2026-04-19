using FluentValidation;
using Microsoft.Extensions.Options;
using Moq;
using PortaBox.Application.Abstractions.Email;
using PortaBox.Application.Abstractions.Identity;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Modules.Gestao.Application.Commands.ResendMagicLink;
using PortaBox.Modules.Gestao.Application.EventHandlers;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class ResendMagicLinkCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_SindicoDoesNotBelongToTenant_ShouldReturnFailure()
    {
        var handler = BuildHandler(
            condominioRepository: CreateCondominioRepository(),
            sindicoRepository: CreateSindicoRepository(sindico: null));

        var result = await handler.HandleAsync(BuildCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResendMagicLinkErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_SindicoAlreadyHasPassword_ShouldReturnFailure()
    {
        var command = BuildCommand();
        var handler = BuildHandler(
            condominioRepository: CreateCondominioRepository(command.CondominioId),
            sindicoRepository: CreateSindicoRepository(CreateSindico(command.CondominioId, command.SindicoUserId)),
            identityUserLookupService: CreateIdentityLookupService(command.CondominioId, command.SindicoUserId, hasPassword: true));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResendMagicLinkErrors.AlreadyHasPassword, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldInvalidateBeforeIssuingNewMagicLink()
    {
        var command = BuildCommand();
        var condominioRepository = CreateCondominioRepository(command.CondominioId);
        var sindicoRepository = CreateSindicoRepository(CreateSindico(command.CondominioId, command.SindicoUserId));
        var identityLookupService = CreateIdentityLookupService(command.CondominioId, command.SindicoUserId, hasPassword: false);
        var magicLinkService = new Mock<IMagicLinkService>(MockBehavior.Strict);
        var emailSender = new Mock<IEmailSender>();
        var emailTemplateRenderer = new Mock<IEmailTemplateRenderer>();
        var auditRepository = new Mock<ITenantAuditRepository>();
        var dbSession = BuildDbSession();
        var sequence = new MockSequence();

        magicLinkService
            .InSequence(sequence)
            .Setup(service => service.CanIssueAsync(command.SindicoUserId, MagicLinkPurpose.PasswordSetup, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MagicLinkIssueResult.Issued(Guid.Empty, command.SindicoUserId, MagicLinkPurpose.PasswordSetup, string.Empty, string.Empty, DateTimeOffset.UtcNow));

        magicLinkService
            .InSequence(sequence)
            .Setup(service => service.InvalidatePendingAsync(command.SindicoUserId, MagicLinkPurpose.PasswordSetup, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        magicLinkService
            .InSequence(sequence)
            .Setup(service => service.IssueAsync(command.SindicoUserId, MagicLinkPurpose.PasswordSetup, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MagicLinkIssueResult.Issued(
                Guid.NewGuid(),
                command.SindicoUserId,
                MagicLinkPurpose.PasswordSetup,
                "raw-token",
                "hash",
                DateTimeOffset.UtcNow.AddHours(72)));

        emailTemplateRenderer
            .Setup(renderer => renderer.Render("MagicLinkPasswordSetup", It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new EmailTemplate("<p>html</p>", "text"));

        emailSender
            .Setup(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        auditRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TenantAuditEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = BuildHandler(
            condominioRepository,
            sindicoRepository,
            identityLookupService,
            magicLinkService,
            emailSender,
            emailTemplateRenderer,
            auditRepository,
            dbSession);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        magicLinkService.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_Success_ShouldSendEmailExactlyOnce()
    {
        var command = BuildCommand();
        var emailSender = new Mock<IEmailSender>();
        var emailTemplateRenderer = new Mock<IEmailTemplateRenderer>();

        emailTemplateRenderer
            .Setup(renderer => renderer.Render("MagicLinkPasswordSetup", It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new EmailTemplate("<p>html</p>", "text"));

        emailSender
            .Setup(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = BuildHandler(
            condominioRepository: CreateCondominioRepository(command.CondominioId),
            sindicoRepository: CreateSindicoRepository(CreateSindico(command.CondominioId, command.SindicoUserId)),
            identityUserLookupService: CreateIdentityLookupService(command.CondominioId, command.SindicoUserId, hasPassword: false),
            magicLinkService: CreateMagicLinkService(command.SindicoUserId),
            emailSender: emailSender,
            emailTemplateRenderer: emailTemplateRenderer,
            tenantAuditRepository: CreateAuditRepository(),
            dbSession: BuildDbSession());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        emailSender.Verify(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ResendMagicLinkCommandHandler BuildHandler(
        Mock<ICondominioRepository>? condominioRepository = null,
        Mock<ISindicoRepository>? sindicoRepository = null,
        Mock<IIdentityUserLookupService>? identityUserLookupService = null,
        Mock<IMagicLinkService>? magicLinkService = null,
        Mock<IEmailSender>? emailSender = null,
        Mock<IEmailTemplateRenderer>? emailTemplateRenderer = null,
        Mock<ITenantAuditRepository>? tenantAuditRepository = null,
        Mock<IApplicationDbSession>? dbSession = null,
        TimeProvider? timeProvider = null)
    {
        return new ResendMagicLinkCommandHandler(
            new ResendMagicLinkCommandValidator(),
            (condominioRepository ?? CreateCondominioRepository()).Object,
            (sindicoRepository ?? CreateSindicoRepository()).Object,
            (identityUserLookupService ?? CreateIdentityLookupService(Guid.NewGuid(), Guid.NewGuid(), false)).Object,
            (magicLinkService ?? CreateMagicLinkService(Guid.NewGuid())).Object,
            (emailSender ?? new Mock<IEmailSender>()).Object,
            (emailTemplateRenderer ?? CreateTemplateRenderer()).Object,
            (tenantAuditRepository ?? CreateAuditRepository()).Object,
            (dbSession ?? BuildDbSession()).Object,
            Options.Create(new CondominioMagicLinkOptions
            {
                SindicoAppBaseUrl = "https://sindico.portabox.test"
            }),
            timeProvider ?? TimeProvider.System);
    }

    private static ResendMagicLinkCommand BuildCommand()
    {
        return new ResendMagicLinkCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }

    private static Mock<ICondominioRepository> CreateCondominioRepository(Guid? condominioId = null)
    {
        var repository = new Mock<ICondominioRepository>();
        if (condominioId.HasValue)
        {
            repository
                .Setup(current => current.GetByIdAsync(condominioId.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Condominio.Create(
                    condominioId.Value,
                    "Residencial Bosque Azul",
                    "12.345.678/0001-95",
                    Guid.NewGuid(),
                    TimeProvider.System));
        }
        else
        {
            repository
                .Setup(current => current.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Condominio?)null);
        }

        return repository;
    }

    private static Mock<ISindicoRepository> CreateSindicoRepository(Sindico? sindico = null)
    {
        var repository = new Mock<ISindicoRepository>();
        repository
            .Setup(current => current.GetByUserIdIgnoreQueryFiltersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sindico);

        return repository;
    }

    private static Mock<IIdentityUserLookupService> CreateIdentityLookupService(Guid tenantId, Guid userId, bool hasPassword)
    {
        var service = new Mock<IIdentityUserLookupService>();
        service
            .Setup(current => current.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityUserLookup(userId, "sindico@portabox.test", hasPassword, tenantId));

        return service;
    }

    private static Mock<IMagicLinkService> CreateMagicLinkService(Guid userId)
    {
        var service = new Mock<IMagicLinkService>();
        service
            .Setup(current => current.CanIssueAsync(userId, MagicLinkPurpose.PasswordSetup, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MagicLinkIssueResult.Issued(Guid.Empty, userId, MagicLinkPurpose.PasswordSetup, string.Empty, string.Empty, DateTimeOffset.UtcNow));
        service
            .Setup(current => current.InvalidatePendingAsync(userId, MagicLinkPurpose.PasswordSetup, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        service
            .Setup(current => current.IssueAsync(userId, MagicLinkPurpose.PasswordSetup, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MagicLinkIssueResult.Issued(
                Guid.NewGuid(),
                userId,
                MagicLinkPurpose.PasswordSetup,
                "raw-token",
                "hash",
                DateTimeOffset.UtcNow.AddHours(72)));

        return service;
    }

    private static Mock<IEmailTemplateRenderer> CreateTemplateRenderer()
    {
        var renderer = new Mock<IEmailTemplateRenderer>();
        renderer
            .Setup(current => current.Render("MagicLinkPasswordSetup", It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new EmailTemplate("<p>html</p>", "text"));

        return renderer;
    }

    private static Mock<ITenantAuditRepository> CreateAuditRepository()
    {
        var repository = new Mock<ITenantAuditRepository>();
        repository
            .Setup(current => current.AddAsync(It.IsAny<TenantAuditEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return repository;
    }

    private static Mock<IApplicationDbSession> BuildDbSession()
    {
        var dbSession = new Mock<IApplicationDbSession>();
        dbSession
            .Setup(session => session.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return dbSession;
    }

    private static Sindico CreateSindico(Guid tenantId, Guid userId)
    {
        return Sindico.Create(
            Guid.NewGuid(),
            tenantId,
            userId,
            "Joao da Silva",
            "+5585999990001",
            TimeProvider.System);
    }
}
