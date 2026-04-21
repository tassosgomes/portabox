using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PortaBox.Application.Abstractions;

namespace PortaBox.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToProblemHttpResult<T>(this Result<T> result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to ProblemDetails.");
        }

        var error = result.Error ?? "Ocorreu um erro inesperado. Tente novamente mais tarde.";
        var descriptor = ProblemDescriptor.FromError(error, StatusCodes.Status400BadRequest);

        return ToProblemHttpResult(httpContext, descriptor, error);
    }

    public static IResult ToValidationProblemHttpResult(this ValidationException exception, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(httpContext);

        var errors = exception.Errors
            .GroupBy(error => ToCamelCase(error.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        return Results.ValidationProblem(
            errors,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Falha de validação",
            type: "https://portabox.app/problems/validation-error",
            detail: "Um ou mais campos estão inválidos",
            instance: httpContext.Request.Path);
    }

    public static IResult ToCreatedHttpResult<T>(this T value, string location)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        return Results.Created(location, value);
    }

    private static IResult ToProblemHttpResult(HttpContext httpContext, ProblemDescriptor descriptor, string detail)
    {
        return Results.Problem(
            statusCode: descriptor.StatusCode,
            title: descriptor.Title,
            type: descriptor.Type,
            detail: detail,
            instance: httpContext.Request.Path);
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return string.Empty;
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }

    private sealed record ProblemDescriptor(int StatusCode, string Title, string Type)
    {
        public static ProblemDescriptor FromError(string error, int defaultStatusCode)
        {
            var normalized = error.Trim();

            if (ContainsAny(normalized, "nao encontrado", "não encontrado", "nao encontrada", "não encontrada"))
            {
                return new ProblemDescriptor(
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado",
                    "https://portabox.app/problems/not-found");
            }

            if (ContainsAny(normalized, "conflito", "ja existe", "já existe", "duplicada"))
            {
                return new ProblemDescriptor(
                    StatusCodes.Status409Conflict,
                    "Conflito canônico",
                    "https://portabox.app/problems/canonical-conflict");
            }

            if (ContainsAny(normalized, "inativo", "inativa", "ja esta ativa", "já está ativa", "ja esta inativa", "já está inativa", "nao e possivel", "não é possível"))
            {
                return new ProblemDescriptor(
                    StatusCodes.Status422UnprocessableEntity,
                    "Transição inválida",
                    "https://portabox.app/problems/invalid-transition");
            }

            return new ProblemDescriptor(
                defaultStatusCode,
                defaultStatusCode == StatusCodes.Status500InternalServerError ? "Erro interno do servidor" : "Falha de validação",
                defaultStatusCode == StatusCodes.Status500InternalServerError
                    ? "https://portabox.app/problems/internal-error"
                    : "https://portabox.app/problems/validation-error");
        }

        private static bool ContainsAny(string value, params string[] fragments)
        {
            return fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }
    }
}
