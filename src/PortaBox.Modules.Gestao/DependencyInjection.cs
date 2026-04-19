using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using FluentValidation;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Modules.Gestao.Application.Commands.ActivateCondominio;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;
using PortaBox.Modules.Gestao.Application.Commands.PasswordSetup;
using PortaBox.Modules.Gestao.Application.Commands.ResendMagicLink;
using PortaBox.Modules.Gestao.Application.Commands.UploadOptInDocument;
using PortaBox.Modules.Gestao.Application.EventHandlers;
using PortaBox.Modules.Gestao.Application.Common;
using PortaBox.Modules.Gestao.Application.Queries.GetCondominioDetails;
using PortaBox.Modules.Gestao.Application.Queries.ListCondominios;
using PortaBox.Application.Abstractions.Events;
using PortaBox.Modules.Gestao.Domain.Events;

namespace PortaBox.Modules.Gestao;

public static class DependencyInjection
{
    public static IServiceCollection AddPortaBoxModuleGestao(this IServiceCollection services, IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = configuration?.GetSection(CondominioMagicLinkOptions.SectionName).Get<CondominioMagicLinkOptions>()
            ?? new CondominioMagicLinkOptions();
        var passwordSetupOptions = configuration?.GetSection(PasswordSetupPolicyOptions.SectionName).Get<PasswordSetupPolicyOptions>()
            ?? new PasswordSetupPolicyOptions();

        services.AddSingleton<IOptions<CondominioMagicLinkOptions>>(Options.Create(options));
        services.AddSingleton<IOptions<PasswordSetupPolicyOptions>>(Options.Create(passwordSetupOptions));
        services.AddScoped<IValidator<ActivateCondominioCommand>, ActivateCondominioCommandValidator>();
        services.AddScoped<IValidator<CreateCondominioCommand>, CreateCondominioCommandValidator>();
        services.AddScoped<IValidator<PasswordSetupCommand>, PasswordSetupCommandValidator>();
        services.AddScoped<IValidator<ResendMagicLinkCommand>, ResendMagicLinkCommandValidator>();
        services.AddScoped<IValidator<UploadOptInDocumentCommand>, UploadOptInDocumentCommandValidator>();
        services.AddScoped<ICommandHandler<ActivateCondominioCommand, ActivateCondominioResult>, ActivateCondominioCommandHandler>();
        services.AddScoped<ICommandHandler<CreateCondominioCommand, CreateCondominioResult>, CreateCondominioCommandHandler>();
        services.AddScoped<ICommandHandler<PasswordSetupCommand, PasswordSetupResult>, PasswordSetupCommandHandler>();
        services.AddScoped<ICommandHandler<ResendMagicLinkCommand, ResendMagicLinkResult>, ResendMagicLinkCommandHandler>();
        services.AddScoped<ICommandHandler<UploadOptInDocumentCommand, UploadOptInDocumentResult>, UploadOptInDocumentCommandHandler>();
        services.AddScoped<IQueryHandler<GetCondominioDetailsQuery, CondominioDetailsDto>, GetCondominioDetailsQueryHandler>();
        services.AddScoped<IQueryHandler<ListCondominiosQuery, PagedResult<CondominioListItemDto>>, ListCondominiosQueryHandler>();
        services.AddScoped<IDomainEventHandler<CondominioCadastradoV1>, SendSindicoMagicLinkOnCondominioCreated>();

        return services;
    }
}
