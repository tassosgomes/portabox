using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Api.Middleware;

namespace PortaBox.Api.IntegrationTests.Security;

/// <summary>
/// Verifies security controls introduced in task_26: rate limiting, security headers,
/// cookie properties, and Data Protection key persistence.
/// </summary>
[Collection(nameof(AppFactoryCollection))]
public sealed class HardeningTests(AppFactoryFixture factory)
{
    // ─── CSP / Security Headers ──────────────────────────────────────────────

    [Fact]
    public async Task LoginEndpoint_ShouldReturn_ContentSecurityPolicyHeader()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "nobody@portabox.test", password = "wrong" });

        Assert.True(response.Headers.Contains("Content-Security-Policy") ||
                    response.Content.Headers.Contains("Content-Security-Policy"),
            "Content-Security-Policy header must be present in every response.");
    }

    [Fact]
    public async Task ApiResponse_ShouldContain_SecurityHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").FirstOrDefault());
        Assert.Equal("strict-origin-when-cross-origin",
            response.Headers.GetValues("Referrer-Policy").FirstOrDefault());
        Assert.Contains(SecurityHeadersMiddleware.CspValue,
            response.Headers.GetValues("Content-Security-Policy").FirstOrDefault() ?? "");
    }

    // ─── Rate Limiting ───────────────────────────────────────────────────────

    [Fact]
    public async Task PasswordSetupEndpoint_ShouldReturn429_AfterExceedingRateLimit()
    {
        // This test uses a dedicated factory with MaxRequests=2 to avoid exhausting
        // the shared fixture's rate limiter and interfering with other tests.
        await using var isolatedFactory = CreateRateLimitTestFactory(maxRequests: 2);
        using var client = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 3; i++)
        {
            lastResponse?.Dispose();
            lastResponse = await client.PostAsJsonAsync("/api/v1/auth/password-setup",
                new { token = "invalid-token", password = "Pass@1234" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
        Assert.True(lastResponse.Headers.Contains("Retry-After"),
            "429 response must include a Retry-After header.");
        lastResponse.Dispose();
    }

    [Fact]
    public async Task LoginEndpoint_ShouldReturn429_AfterExceedingRateLimit()
    {
        await using var isolatedFactory = CreateRateLimitTestFactory(maxRequests: 2);
        using var client = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 3; i++)
        {
            lastResponse?.Dispose();
            lastResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { email = "nobody@portabox.test", password = "wrong" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
        Assert.True(lastResponse.Headers.Contains("Retry-After"));
        lastResponse.Dispose();
    }

    // ─── Data Protection key persistence ─────────────────────────────────────

    [Fact]
    public async Task AuthCookie_ShouldRemainValid_AfterApplicationRestart_WithPersistentKeys()
    {
        await factory.ResetAsync();
        var keysDir = Path.Combine(Path.GetTempPath(), $"portabox-dp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(keysDir);

        try
        {
            string? setCookieHeader;

            // First factory lifetime: login and capture the auth cookie.
            await using (var firstFactory = factory.CreateIsolatedFactoryWithDataProtection(keysDir))
            {
                using var client = firstFactory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = false  // capture raw Set-Cookie header
                });

                var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login",
                    new { email = "operator@portabox.dev", password = "PortaBox123!" });
                Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

                setCookieHeader = loginResp.Headers
                    .GetValues("Set-Cookie")
                    .FirstOrDefault(h => h.Contains("portabox.auth"));
                Assert.NotNull(setCookieHeader);
            }

            // Second factory lifetime (simulates restart): send the old cookie.
            await using var secondFactory = factory.CreateIsolatedFactoryWithDataProtection(keysDir);
            using var client2 = secondFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

            // Extract cookie value from Set-Cookie header (name=value; ...)
            var cookieValue = setCookieHeader!.Split(';')[0];  // "portabox.auth=<value>"
            client2.DefaultRequestHeaders.Add("Cookie", cookieValue);

            var meResp = await client2.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);
        }
        finally
        {
            if (Directory.Exists(keysDir))
                Directory.Delete(keysDir, recursive: true);
        }
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private WebApplicationFactory<Program> CreateRateLimitTestFactory(int maxRequests)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings__Postgres"] = factory.PostgresConnectionString,
            ["Persistence__ApplyMigrationsOnStartup"] = "false",
            ["Email__UseStartTls"] = "false",
            ["DomainEvents__Publisher__Enabled"] = "false",
            ["RateLimiting__Auth__MaxRequests"] = maxRequests.ToString(),
            ["RateLimiting__Auth__WindowMinutes"] = "10",
            ["CondominioMagicLink__SindicoAppBaseUrl"] = "https://sindico.portabox.test",
            ["Storage__Provider"] = "Minio",
            ["Storage__Endpoint"] = factory.MinioEndpoint,
            ["Storage__BucketName"] = factory.MinioBucketName,
            ["Storage__AccessKey"] = "minioadmin",
            ["Storage__SecretKey"] = "minioadmin",
            ["Storage__UseSsl"] = "false",
            ["Storage__ForcePathStyle"] = "true",
            ["Storage__Region"] = "us-east-1",
        };

        return new EnvVarWebApplicationFactory("Development", settings);
    }

    private sealed class EnvVarWebApplicationFactory(
        string environment,
        IReadOnlyDictionary<string, string?> settings)
        : WebApplicationFactory<Program>
    {
        private readonly Dictionary<string, string?> _prev = [];

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            foreach (var (key, value) in settings)
            {
                _prev[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
            builder.UseEnvironment(environment);
        }

        protected override void Dispose(bool disposing) { Restore(); base.Dispose(disposing); }
        public override async ValueTask DisposeAsync() { Restore(); await base.DisposeAsync(); }

        private void Restore()
        {
            foreach (var (key, prev) in _prev)
                Environment.SetEnvironmentVariable(key, prev);
        }
    }
}
