using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Api.Extensions;
using PortaBox.Application.Abstractions;
using System.Text.Json;

namespace PortaBox.Api.UnitTests;

public sealed class ResultExtensionsTests
{
    [Fact]
    public async Task ToProblemHttpResult_WithNotFoundError_Returns404ProblemDetails()
    {
        var result = Result<string>.Failure("Bloco não encontrado");
        var httpContext = CreateHttpContext("/api/v1/condominios/1/blocos/2");

        await result.ToProblemHttpResult(httpContext).ExecuteAsync(httpContext);

        var problemDetails = await DeserializeProblemDetailsAsync(httpContext);
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Equal("Recurso não encontrado", problemDetails.Title);
        Assert.Equal("Bloco não encontrado", problemDetails.Detail);
    }

    [Fact]
    public async Task ToProblemHttpResult_WithConflictError_Returns409ProblemDetails()
    {
        var result = Result<string>.Failure("Já existe bloco ativo com este nome");
        var httpContext = CreateHttpContext("/api/v1/condominios/1/blocos");

        await result.ToProblemHttpResult(httpContext).ExecuteAsync(httpContext);

        var problemDetails = await DeserializeProblemDetailsAsync(httpContext);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.Equal("Conflito canônico", problemDetails.Title);
        Assert.Equal("Já existe bloco ativo com este nome", problemDetails.Detail);
    }

    [Fact]
    public async Task ToProblemHttpResult_WithInvalidTransitionError_Returns422ProblemDetails()
    {
        var result = Result<string>.Failure("não é possível renomear bloco inativo");
        var httpContext = CreateHttpContext("/api/v1/condominios/1/blocos/2");

        await result.ToProblemHttpResult(httpContext).ExecuteAsync(httpContext);

        var problemDetails = await DeserializeProblemDetailsAsync(httpContext);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, httpContext.Response.StatusCode);
        Assert.Equal("Transição inválida", problemDetails.Title);
        Assert.Equal("não é possível renomear bloco inativo", problemDetails.Detail);
    }

    [Fact]
    public async Task ToValidationProblemHttpResult_Returns400ValidationProblemDetails()
    {
        var exception = new ValidationException(
            [
                new ValidationFailure("Nome", "O nome é obrigatório"),
                new ValidationFailure("Andar", "O andar deve ser maior ou igual a zero")
            ]);
        var httpContext = CreateHttpContext("/api/v1/condominios/1/blocos");

        await exception.ToValidationProblemHttpResult(httpContext).ExecuteAsync(httpContext);

        var problemDetails = await DeserializeValidationProblemDetailsAsync(httpContext);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.Equal("Falha de validação", problemDetails.Title);
        Assert.Equal("Um ou mais campos estão inválidos", problemDetails.Detail);
        Assert.Contains("O nome é obrigatório", problemDetails.Errors["nome"]);
        Assert.Contains("O andar deve ser maior ou igual a zero", problemDetails.Errors["andar"]);
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();
        httpContext.Request.Path = path;
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<ProblemDetails> DeserializeProblemDetailsAsync(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        return (await JsonSerializer.DeserializeAsync<ProblemDetails>(
            httpContext.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
    }

    private static async Task<ValidationProblemDetails> DeserializeValidationProblemDetailsAsync(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        return (await JsonSerializer.DeserializeAsync<ValidationProblemDetails>(
            httpContext.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
    }
}
