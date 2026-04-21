using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PortaBox.Api.Infrastructure;

public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private const string ValidationErrorType = "https://portabox.app/problems/validation-error";
    private const string InternalServerErrorType = "https://portabox.app/problems/internal-error";

    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;

    public ProblemDetailsExceptionHandler(
        ProblemDetailsFactory problemDetailsFactory,
        IHostEnvironment environment,
        ILogger<ProblemDetailsExceptionHandler> logger)
    {
        _problemDetailsFactory = problemDetailsFactory;
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        _logger.LogError(exception, "Unhandled exception while processing request {RequestPath}.", httpContext.Request.Path);

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? string.Empty : error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.ErrorMessage)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray(),
                    StringComparer.Ordinal);

            var modelState = new ModelStateDictionary();

            foreach (var error in errors)
            {
                foreach (var errorMessage in error.Value)
                {
                    modelState.AddModelError(error.Key, errorMessage);
                }
            }

            var validationProblemDetails = _problemDetailsFactory.CreateValidationProblemDetails(
                httpContext,
                modelState,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Falha de validação",
                type: ValidationErrorType,
                detail: "Um ou mais campos estão inválidos",
                instance: httpContext.Request.Path);

            validationProblemDetails.Extensions["traceId"] =
                Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            await httpContext.Response.WriteAsJsonAsync(validationProblemDetails, cancellationToken);
            return true;
        }

        var detail = _environment.IsDevelopment()
            ? exception.Message
            : "Ocorreu um erro inesperado. Tente novamente mais tarde.";

        var problemDetails = _problemDetailsFactory.CreateProblemDetails(
            httpContext,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Erro interno do servidor",
            type: InternalServerErrorType,
            detail: detail,
            instance: httpContext.Request.Path);

        problemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
