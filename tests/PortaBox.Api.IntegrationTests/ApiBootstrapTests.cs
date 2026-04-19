using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PortaBox.Api.IntegrationTests;

public class ApiBootstrapTests
{
    [Fact]
    public async Task HealthLive_ShouldReturnHealthyJsonPayload()
    {
        await using var factory = CreateFactory(
            "Development",
            new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Minio",
                ["Storage:BucketName"] = "log-portaria-dev",
                ["Storage:Endpoint"] = "http://localhost:9000",
                ["Storage:AccessKey"] = "minioadmin",
                ["Storage:SecretKey"] = "minioadmin",
                ["Storage:UseSsl"] = "false",
                ["Email:Host"] = "localhost",
                ["Email:Port"] = "1025",
                ["DomainEvents:Publisher:Enabled"] = "false"
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.True(response.IsSuccessStatusCode);
        var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("Healthy", payload.RootElement.GetProperty("status").GetString());
        Assert.True(payload.RootElement.TryGetProperty("checks", out _));
    }

    [Fact]
    public async Task GetUnknownApiRoute_ShouldReturnProblemDetails404WithTraceId()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/healthz-nonexistent");

        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problemDetails = await DeserializeAsync<ProblemDetails>(response);

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("/api/v1/healthz-nonexistent", problemDetails.Instance);
        Assert.False(string.IsNullOrWhiteSpace(problemDetails.Type));
        Assert.False(string.IsNullOrWhiteSpace(problemDetails.Title));
        Assert.False(string.IsNullOrWhiteSpace(problemDetails.Extensions["traceId"]?.ToString()));
    }

    [Fact]
    public async Task ThrowEndpointInProduction_ShouldReturn500ProblemDetailsWithoutStackTrace()
    {
        await using var factory = CreateFactory(
            "Production",
            new Dictionary<string, string?>
            {
                ["Testing:EnableExceptionEndpoint"] = "true",
                ["Cors:AllowedOrigins:0"] = "https://admin.portabox.com"
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/_throw");

        Assert.Equal(StatusCodes.Status500InternalServerError, (int)response.StatusCode);

        var problemDetails = await DeserializeAsync<ProblemDetails>(response);

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("An unexpected error occurred.", problemDetails.Title);
        Assert.Equal("An unexpected error occurred.", problemDetails.Detail);
        Assert.DoesNotContain("InvalidOperationException", problemDetails.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", problemDetails.Detail, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(problemDetails.Extensions["traceId"]?.ToString()));
    }

    [Fact]
    public async Task RequestLogs_ShouldContainRequestIdAndTraceIdInSameLogEvent()
    {
        var originalOut = Console.Out;
        await using var writer = new StringWriter(new StringBuilder());

        try
        {
            Console.SetOut(writer);

            await using var factory = CreateFactory("Development");
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/health/live");

            Assert.True(response.IsSuccessStatusCode);

            await writer.FlushAsync();
            var logLines = writer.ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            var requestLog = logLines
                .Select(line => JsonDocument.Parse(line).RootElement)
                .Last(element =>
                    element.TryGetProperty("message", out var message) &&
                    message.GetString() == "Request correlation established for \"/health/live\".");

            Assert.True(requestLog.TryGetProperty("request_id", out var requestId));
            Assert.True(requestLog.TryGetProperty("trace_id", out var traceId));
            Assert.True(requestLog.TryGetProperty("span_id", out var spanId));
            Assert.False(string.IsNullOrWhiteSpace(requestId.GetString()));
            Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
            Assert.False(string.IsNullOrWhiteSpace(spanId.GetString()));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void RequestLogs_ShouldSanitizeSensitiveFieldsInFormatter()
    {
        var formatter = new PortaBox.Api.Infrastructure.ApiJsonFormatter();
        var logEvent = new Serilog.Events.LogEvent(
            DateTimeOffset.UtcNow,
            Serilog.Events.LogEventLevel.Warning,
            null,
            Serilog.Events.MessageTemplate.Empty,
            [new Serilog.Events.LogEventProperty("token", new Serilog.Events.ScalarValue("abc123"))]);

        using var writer = new StringWriter();
        formatter.Format(logEvent, writer);

        var json = writer.ToString();
        Assert.DoesNotContain("abc123", json, StringComparison.Ordinal);
        Assert.Contains("***", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Swagger_ShouldBeAvailableOnlyInDevelopment()
    {
        await using var developmentFactory = CreateFactory("Development");
        using var developmentClient = developmentFactory.CreateClient();
        var developmentResponse = await developmentClient.GetAsync("/swagger/index.html");
        Assert.True(developmentResponse.IsSuccessStatusCode);

        await using var productionFactory = CreateFactory("Production");
        using var productionClient = productionFactory.CreateClient();
        var productionResponse = await productionClient.GetAsync("/swagger/index.html");
        Assert.Equal(StatusCodes.Status404NotFound, (int)productionResponse.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        string environment,
        Dictionary<string, string?>? settings = null)
    {
        var mergedSettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings__Postgres"] = "Host=localhost;Port=5432;Database=portabox_bootstrap;Username=postgres;Password=postgres",
            ["Persistence__ApplyMigrationsOnStartup"] = "false"
        };

        if (settings is not null)
        {
            foreach (var pair in settings)
            {
                mergedSettings[pair.Key.Replace(":", "__")] = pair.Value;
            }
        }

        return new ConfiguredWebApplicationFactory(environment, mergedSettings);
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<T>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private sealed class ConfiguredWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _environment;
        private readonly Dictionary<string, string?> _previousValues = [];

        public ConfiguredWebApplicationFactory(
            string environment,
            IReadOnlyDictionary<string, string?> settings)
        {
            _environment = environment;
            ClientOptions.BaseAddress = new Uri("http://localhost");

            foreach (var pair in settings)
            {
                _previousValues[pair.Key] = Environment.GetEnvironmentVariable(pair.Key);
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);
        }

        protected override void Dispose(bool disposing)
        {
            RestoreEnvironmentVariables();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            RestoreEnvironmentVariables();
            await base.DisposeAsync();
        }

        private void RestoreEnvironmentVariables()
        {
            foreach (var pair in _previousValues)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
    }
}
