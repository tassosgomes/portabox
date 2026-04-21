using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using PortaBox.Application.Abstractions.Events;
using PortaBox.Application.Abstractions.Identity;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Infrastructure.Audit;
using PortaBox.Infrastructure.Email;
using PortaBox.Infrastructure.Events;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MagicLinks;
using PortaBox.Infrastructure.MultiTenancy;
using PortaBox.Infrastructure.Observability;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Infrastructure.Repositories;
using PortaBox.Infrastructure.Storage;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Application.Unidades;

namespace PortaBox.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Postgres' was not configured.");
        }

        services
            .AddOptions<IdentityConfiguration>()
            .Bind(configuration.GetSection(IdentityConfiguration.SectionName));

        var magicLinkOptions = configuration.GetSection(MagicLinkOptions.SectionName).Get<MagicLinkOptions>() ?? new MagicLinkOptions();
        services.AddSingleton<IOptions<MagicLinkOptions>>(Options.Create(magicLinkOptions));

        var domainEventPublisherOptions = configuration.GetSection(DomainEventPublisherOptions.SectionName).Get<DomainEventPublisherOptions>() ?? new DomainEventPublisherOptions();
        services.AddSingleton<IOptions<DomainEventPublisherOptions>>(Options.Create(domainEventPublisherOptions));

        services.AddObjectStorage(configuration);
        services.AddEmailInfrastructure(configuration);
        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton(_ => CreateDataSource(connectionString));

        // ITenantContext is scoped per request; TenantResolutionMiddleware populates it.
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IApplicationDbSession, ApplicationDbSession>();
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();
        services.AddScoped<IIdentityPasswordService, IdentityPasswordService>();
        services.AddScoped<DomainEventOutboxInterceptor>();
        services.AddScoped<DomainEventOutboxProcessor>();
        services.AddSingleton<GestaoMetrics>();
        services.AddSingleton<IGestaoMetrics>(serviceProvider => serviceProvider.GetRequiredService<GestaoMetrics>());
        services.AddScoped<IIdentityUserProvisioningService, IdentityUserProvisioningService>();
        services.AddScoped<IIdentityUserLookupService, IdentityUserLookupService>();

        if (domainEventPublisherOptions.Enabled)
        {
            services.AddHostedService<DomainEventOutboxPublisher>();
        }

        // AddDbContext (not AddDbContextPool) is required here because AppDbContext receives a
        // scoped ITenantContext via constructor injection. DbContext pooling reuses instances
        // across requests and is therefore incompatible with scoped constructor dependencies.
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options
                .UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>())
                .AddInterceptors(serviceProvider.GetRequiredService<DomainEventOutboxInterceptor>())
                .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .UseSnakeCaseNamingConvention();
        });

        services
            .AddIdentityCore<AppUser>(options =>
            {
                var passwordPolicy = configuration
                    .GetSection(IdentityConfiguration.SectionName)
                    .Get<IdentityConfiguration>()?
                    .Password ?? new PasswordPolicyConfiguration();

                options.Password.RequireDigit = passwordPolicy.RequireDigit;
                options.Password.RequireLowercase = passwordPolicy.RequireLowercase;
                options.Password.RequireUppercase = passwordPolicy.RequireUppercase;
                options.Password.RequireNonAlphanumeric = passwordPolicy.RequireNonAlphanumeric;
                options.Password.RequiredLength = passwordPolicy.RequiredLength;
                options.Password.RequiredUniqueChars = passwordPolicy.RequiredUniqueChars;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager();

        services.AddScoped<IdentitySeeder>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IBlocoRepository, BlocoRepository>();
        services.AddScoped<ICondominioRepository, CondominioRepository>();
        services.AddScoped<IMagicLinkService, MagicLinkService>();
        services.AddScoped<IOptInDocumentRepository, OptInDocumentRepository>();
        services.AddScoped<IOptInRecordRepository, OptInRecordRepository>();
        services.AddScoped<ISindicoRepository, SindicoRepository>();
        services.AddScoped<ITenantAuditRepository, TenantAuditRepository>();
        services.AddScoped<IUnidadeRepository, UnidadeRepository>();

        return services;
    }

    public static IServiceCollection AddPortaBoxInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddInfrastructure(configuration);
    }

    internal static NpgsqlDataSource CreateDataSource(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        return dataSourceBuilder.Build();
    }
}
