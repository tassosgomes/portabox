using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions.Email;
using PortaBox.Application.Abstractions.Events;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;
using PortaBox.Modules.Gestao.Domain.Events;

namespace PortaBox.Modules.Gestao.Application.EventHandlers;

public sealed class SendSindicoMagicLinkOnCondominioCreated(
    IMagicLinkService magicLinkService,
    IEmailSender emailSender,
    IEmailTemplateRenderer emailTemplateRenderer,
    IOptions<CondominioMagicLinkOptions> optionsAccessor) : IDomainEventHandler<CondominioCadastradoV1>
{
    private readonly CondominioMagicLinkOptions _options = optionsAccessor.Value;

    public async Task HandleAsync(CondominioCadastradoV1 domainEvent, CancellationToken cancellationToken = default)
    {
        var issueResult = await magicLinkService.IssueAsync(
            domainEvent.SindicoUserId,
            MagicLinkPurpose.PasswordSetup,
            null,
            cancellationToken);

        if (!issueResult.IsSuccess || string.IsNullOrWhiteSpace(issueResult.RawToken))
        {
            throw new InvalidOperationException(CreateCondominioErrors.MagicLinkIssueFailed);
        }

        var link = BuildSetupPasswordLink(issueResult.RawToken);
        var template = emailTemplateRenderer.Render(
            "MagicLinkPasswordSetup",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["nome"] = domainEvent.SindicoNomeCompleto,
                ["nome_condominio"] = domainEvent.NomeFantasia,
                ["link"] = link
            });

        await emailSender.SendAsync(
            new EmailMessage(
                domainEvent.SindicoEmail,
                "Bem-vindo ao PortaBox — defina sua senha",
                template.HtmlBody,
                template.TextBody,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["template"] = "MagicLinkPasswordSetup"
                }),
            cancellationToken);
    }

    private string BuildSetupPasswordLink(string rawToken)
    {
        return $"{_options.SindicoAppBaseUrl.TrimEnd('/')}/password-setup?token={Uri.EscapeDataString(rawToken)}";
    }
}
