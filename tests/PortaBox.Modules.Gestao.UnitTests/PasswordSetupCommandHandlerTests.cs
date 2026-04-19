using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PortaBox.Application.Abstractions.Identity;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Modules.Gestao.Application.Commands.PasswordSetup;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class PasswordSetupCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_TokenNotFound_ShouldReturnGenericFailure()
    {
        var command = BuildCommand();
        var magicLinkService = new Mock<IMagicLinkService>();
        magicLinkService
            .Setup(service => service.ValidateAndConsumeAsync(command.RawToken, MagicLinkPurpose.PasswordSetup, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MagicLinkConsumeResult.Invalid(MagicLinkPurpose.PasswordSetup, MagicLinkFailureReason.NotFound));

        var handler = BuildHandler(magicLinkService: magicLinkService);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PasswordSetupErrors.Generic, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ValidTokenAndPassword_ShouldReturnSuccess()
    {
        var command = BuildCommand();
        var userId = Guid.NewGuid();
        var magicLinkService = new Mock<IMagicLinkService>();
        var identityPasswordService = new Mock<IIdentityPasswordService>();
        var dbSession = BuildDbSession();

        magicLinkService
            .Setup(service => service.ValidateAndConsumeAsync(command.RawToken, MagicLinkPurpose.PasswordSetup, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MagicLinkConsumeResult.Consumed(Guid.NewGuid(), userId, MagicLinkPurpose.PasswordSetup));

        identityPasswordService
            .Setup(service => service.AddPasswordAsync(userId, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SetPasswordResult.Success());

        var handler = BuildHandler(
            magicLinkService: magicLinkService,
            identityPasswordService: identityPasswordService,
            dbSession: dbSession);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        identityPasswordService.VerifyAll();
    }

    private static PasswordSetupCommandHandler BuildHandler(
        Mock<IMagicLinkService>? magicLinkService = null,
        Mock<IIdentityPasswordService>? identityPasswordService = null,
        Mock<IApplicationDbSession>? dbSession = null)
    {
        return new PasswordSetupCommandHandler(
            new PasswordSetupCommandValidator(Options.Create(new PasswordSetupPolicyOptions())),
            (magicLinkService ?? CreateMagicLinkService()).Object,
            (identityPasswordService ?? CreateIdentityPasswordService()).Object,
            (dbSession ?? BuildDbSession()).Object,
            NullLogger<PasswordSetupCommandHandler>.Instance);
    }

    private static PasswordSetupCommand BuildCommand()
    {
        return new PasswordSetupCommand("raw-token", "abcde12345", "127.0.0.1");
    }

    private static Mock<IMagicLinkService> CreateMagicLinkService()
    {
        var service = new Mock<IMagicLinkService>();
        service
            .Setup(current => current.ValidateAndConsumeAsync(It.IsAny<string>(), MagicLinkPurpose.PasswordSetup, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MagicLinkConsumeResult.Consumed(Guid.NewGuid(), Guid.NewGuid(), MagicLinkPurpose.PasswordSetup));
        return service;
    }

    private static Mock<IIdentityPasswordService> CreateIdentityPasswordService()
    {
        var service = new Mock<IIdentityPasswordService>();
        service
            .Setup(current => current.AddPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SetPasswordResult.Success());
        return service;
    }

    private static Mock<IApplicationDbSession> BuildDbSession()
    {
        var transaction = new Mock<IApplicationDbTransaction>();
        transaction
            .Setup(current => current.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        transaction
            .Setup(current => current.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var dbSession = new Mock<IApplicationDbSession>();
        dbSession
            .Setup(current => current.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        return dbSession;
    }
}
