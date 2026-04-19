using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PortaBox.Api.Infrastructure;
using PortaBox.Api.Options;

namespace PortaBox.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "ApiCors";

    public static IServiceCollection AddPortaBoxApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.Configure<CorsSettings>(configuration.GetSection(CorsSettings.SectionName));

        services.AddControllers();
        services.AddHttpContextAccessor();
        services.AddPortaBoxLogEnrichers();
        services.AddIdentityBaseline(configuration, environment);
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
            };
        });

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod();

                if (environment.IsDevelopment())
                {
                    policy.AllowAnyOrigin();
                    return;
                }

                var corsSettings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();

                if (corsSettings.AllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(corsSettings.AllowedOrigins);
                }
            });
        });

        services.AddPortaBoxRateLimiting(configuration);
        services.AddEndpointsApiExplorer();

        if (environment.IsDevelopment())
        {
            services.AddSwaggerGen();
        }

        return services;
    }
}
