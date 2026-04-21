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

                if (ShouldNormalizeType(context.ProblemDetails.Type))
                {
                    context.ProblemDetails.Type = GetProblemType(context.ProblemDetails.Status);
                }

                if (ShouldNormalizeTitle(context.ProblemDetails.Title))
                {
                    context.ProblemDetails.Title = GetProblemTitle(context.ProblemDetails.Status);
                }

                if (ShouldNormalizeDetail(context.ProblemDetails.Detail))
                {
                    context.ProblemDetails.Detail = GetProblemDetail(context.ProblemDetails.Status);
                }

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

    private static string GetProblemType(int? statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "https://portabox.app/problems/validation-error",
            StatusCodes.Status401Unauthorized => "https://portabox.app/problems/unauthorized",
            StatusCodes.Status403Forbidden => "https://portabox.app/problems/forbidden",
            StatusCodes.Status404NotFound => "https://portabox.app/problems/not-found",
            StatusCodes.Status409Conflict => "https://portabox.app/problems/canonical-conflict",
            StatusCodes.Status422UnprocessableEntity => "https://portabox.app/problems/invalid-transition",
            StatusCodes.Status500InternalServerError => "https://portabox.app/problems/internal-error",
            _ => "about:blank"
        };
    }

    private static string GetProblemTitle(int? statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Falha de validação",
            StatusCodes.Status401Unauthorized => "Não autorizado",
            StatusCodes.Status403Forbidden => "Acesso negado",
            StatusCodes.Status404NotFound => "Recurso não encontrado",
            StatusCodes.Status409Conflict => "Conflito canônico",
            StatusCodes.Status422UnprocessableEntity => "Transição inválida",
            StatusCodes.Status500InternalServerError => "Erro interno do servidor",
            _ => "Erro na requisição"
        };
    }

    private static string? GetProblemDetail(int? statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Um ou mais campos estão inválidos",
            StatusCodes.Status401Unauthorized => "Token de autenticação inválido ou expirado",
            StatusCodes.Status403Forbidden => "Você não tem permissão para executar esta operação",
            StatusCodes.Status404NotFound => "O recurso solicitado não foi encontrado",
            StatusCodes.Status409Conflict => "A operação entrou em conflito com o estado atual do recurso",
            StatusCodes.Status422UnprocessableEntity => "A operação não pode ser executada no estado atual do recurso",
            StatusCodes.Status500InternalServerError => "Ocorreu um erro inesperado. Tente novamente mais tarde.",
            _ => null
        };
    }

    private static bool ShouldNormalizeType(string? type)
    {
        return string.IsNullOrWhiteSpace(type) || string.Equals(type, "about:blank", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldNormalizeTitle(string? title)
    {
        return string.IsNullOrWhiteSpace(title)
            || title is "Unauthorized" or "Forbidden" or "Not Found" or "Bad Request" or "One or more errors occurred.";
    }

    private static bool ShouldNormalizeDetail(string? detail)
    {
        return string.IsNullOrWhiteSpace(detail)
            || detail is "Unauthorized" or "Forbidden" or "Not Found" or "Bad Request";
    }
}
