using FluentValidation;
using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Email;
using PortaBox.Application.Abstractions.Identity;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Modules.Gestao.Application.EventHandlers;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Commands.ResendMagicLink;

public sealed class ResendMagicLinkCommandHandler(
    IValidator<ResendMagicLinkCommand> validator,
    ICondominioRepository condominioRepository,
    ISindicoRepository sindicoRepository,
    IIdentityUserLookupService identityUserLookupService,
    IMagicLinkService magicLinkService,
    IEmailSender emailSender,
    IEmailTemplateRenderer emailTemplateRenderer,
    ITenantAuditRepository tenantAuditRepository,
    IApplicationDbSession dbSession,
    IOptions<CondominioMagicLinkOptions> optionsAccessor,
    TimeProvider timeProvider) : ICommandHandler<ResendMagicLinkCommand, ResendMagicLinkResult>
{
    private readonly CondominioMagicLinkOptions _options = optionsAccessor.Value;

    public async Task<Result<ResendMagicLinkResult>> HandleAsync(ResendMagicLinkCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var condominio = await condominioRepository.GetByIdAsync(command.CondominioId, cancellationToken);
        if (condominio is null)
        {
            return Result<ResendMagicLinkResult>.Failure(ResendMagicLinkErrors.NotFound);
        }

        var sindico = await sindicoRepository.GetByUserIdIgnoreQueryFiltersAsync(command.SindicoUserId, cancellationToken);
        if (sindico is null || sindico.TenantId != command.CondominioId)
        {
            return Result<ResendMagicLinkResult>.Failure(ResendMagicLinkErrors.NotFound);
        }

        var user = await identityUserLookupService.GetByIdAsync(command.SindicoUserId, cancellationToken);
        if (user is null || user.SindicoTenantId != command.CondominioId)
        {
            return Result<ResendMagicLinkResult>.Failure(ResendMagicLinkErrors.NotFound);
        }

        if (user.HasPassword)
        {
            return Result<ResendMagicLinkResult>.Failure(ResendMagicLinkErrors.AlreadyHasPassword);
        }

        var issueEligibility = await magicLinkService.CanIssueAsync(
            command.SindicoUserId,
            MagicLinkPurpose.PasswordSetup,
            cancellationToken);

        if (!issueEligibility.IsSuccess)
        {
            return Result<ResendMagicLinkResult>.Failure(ResendMagicLinkErrors.RateLimited);
        }

        await magicLinkService.InvalidatePendingAsync(command.SindicoUserId, MagicLinkPurpose.PasswordSetup, cancellationToken);

        var issueResult = await magicLinkService.IssueAsync(
            command.SindicoUserId,
            MagicLinkPurpose.PasswordSetup,
            null,
            cancellationToken);

        if (issueResult.FailureReason == MagicLinkFailureReason.RateLimited)
        {
            return Result<ResendMagicLinkResult>.Failure(ResendMagicLinkErrors.RateLimited);
        }

        if (!issueResult.IsSuccess || string.IsNullOrWhiteSpace(issueResult.RawToken))
        {
            return Result<ResendMagicLinkResult>.Failure(ResendMagicLinkErrors.MagicLinkIssueFailed);
        }

        var template = emailTemplateRenderer.Render(
            "MagicLinkPasswordSetup",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["nome"] = sindico.NomeCompleto,
                ["nome_condominio"] = condominio.NomeFantasia,
                ["link"] = BuildSetupPasswordLink(issueResult.RawToken)
            });

        await emailSender.SendAsync(
            new EmailMessage(
                user.Email,
                "Bem-vindo ao PortaBox — defina sua senha",
                template.HtmlBody,
                template.TextBody,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["template"] = "MagicLinkPasswordSetup"
                }),
            cancellationToken);

        await tenantAuditRepository.AddAsync(
            TenantAuditEntry.Create(
                command.CondominioId,
                TenantAuditEventKind.MagicLinkResent,
                command.PerformedByUserId,
                timeProvider.GetUtcNow()),
            cancellationToken);

        await dbSession.SaveChangesAsync(cancellationToken);

        return Result<ResendMagicLinkResult>.Success(new ResendMagicLinkResult(command.SindicoUserId));
    }

    private string BuildSetupPasswordLink(string rawToken)
    {
        return $"{_options.SindicoAppBaseUrl.TrimEnd('/')}/password-setup?token={Uri.EscapeDataString(rawToken)}";
    }
}
