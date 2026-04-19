using Microsoft.Extensions.Options;
using Moq;
using PortaBox.Application.Abstractions.Email;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Modules.Gestao.Application.EventHandlers;
using PortaBox.Modules.Gestao.Domain.Events;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class SendSindicoMagicLinkOnCondominioCreatedTests
{
    [Fact]
    public async Task HandleAsync_ShouldIssuePasswordSetupMagicLinkAndSendEmail()
    {
        var magicLinkService = new Mock<IMagicLinkService>();
        var emailSender = new Mock<IEmailSender>();
        var renderer = new Mock<IEmailTemplateRenderer>();

        magicLinkService
            .Setup(service => service.IssueAsync(
                It.IsAny<Guid>(),
                MagicLinkPurpose.PasswordSetup,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MagicLinkIssueResult.Issued(
                Guid.NewGuid(),
                Guid.NewGuid(),
                MagicLinkPurpose.PasswordSetup,
                "raw-token",
                "hash",
                DateTimeOffset.UtcNow.AddHours(72)));

        renderer
            .Setup(current => current.Render("MagicLinkPasswordSetup", It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new EmailTemplate("<p>html</p>", "text"));

        var handler = new SendSindicoMagicLinkOnCondominioCreated(
            magicLinkService.Object,
            emailSender.Object,
            renderer.Object,
            Options.Create(new CondominioMagicLinkOptions
            {
                SindicoAppBaseUrl = "https://sindico.portabox.test"
            }));

        await handler.HandleAsync(new CondominioCadastradoV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Residencial Bosque Azul",
            "12345678000195",
            "Joao da Silva",
            "sindico@portabox.test",
            DateTimeOffset.UtcNow));

        magicLinkService.Verify(service => service.IssueAsync(
            It.IsAny<Guid>(),
            MagicLinkPurpose.PasswordSetup,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        emailSender.Verify(sender => sender.SendAsync(
            It.Is<EmailMessage>(message =>
                message.To == "sindico@portabox.test" &&
                message.Subject == "Bem-vindo ao PortaBox — defina sua senha" &&
                message.HtmlBody.Contains("html", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
