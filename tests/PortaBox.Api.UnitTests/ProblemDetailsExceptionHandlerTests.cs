using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using PortaBox.Api.Infrastructure;

namespace PortaBox.Api.UnitTests;

public class ProblemDetailsExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WithValidationException_ReturnsBadRequestProblemDetailsWithErrors()
    {
        using var activity = new Activity("validation-test").Start();

        var handler = new ProblemDetailsExceptionHandler(
            TestHostProblemDetailsFactory.Create(),
            new FakeHostEnvironment(Environments.Production),
            NullLogger<ProblemDetailsExceptionHandler>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/admin/condominios";
        httpContext.Response.Body = new MemoryStream();

        var exception = new ValidationException(
            [
                new ValidationFailure("Cnpj", "CNPJ is required."),
                new ValidationFailure("NomeFantasia", "Name is required.")
            ]);

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ValidationProblemDetails>(
            httpContext.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("/api/v1/admin/condominios", problemDetails.Instance);
        Assert.Equal("https://portabox.app/problems/validation-error", problemDetails.Type);
        Assert.Equal("Falha de validação", problemDetails.Title);
        Assert.Contains("CNPJ is required.", problemDetails.Errors["Cnpj"]);
        Assert.Contains("Name is required.", problemDetails.Errors["NomeFantasia"]);
        Assert.Equal(activity.TraceId.ToString(), problemDetails.Extensions["traceId"]?.ToString());
    }

    [Fact]
    public async Task TryHandleAsync_WithUnhandledException_ReturnsInternalServerErrorProblemDetailsWithTraceId()
    {
        using var activity = new Activity("exception-test").Start();

        var handler = new ProblemDetailsExceptionHandler(
            TestHostProblemDetailsFactory.Create(),
            new FakeHostEnvironment(Environments.Production),
            NullLogger<ProblemDetailsExceptionHandler>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/_throw";
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new Exception("Should not leak."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            httpContext.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("/api/v1/_throw", problemDetails.Instance);
        Assert.Equal("https://portabox.app/problems/internal-error", problemDetails.Type);
        Assert.Equal("Erro interno do servidor", problemDetails.Title);
        Assert.Equal("Ocorreu um erro inesperado. Tente novamente mais tarde.", problemDetails.Detail);
        Assert.Equal(activity.TraceId.ToString(), problemDetails.Extensions["traceId"]?.ToString());
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "PortaBox.Api.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
