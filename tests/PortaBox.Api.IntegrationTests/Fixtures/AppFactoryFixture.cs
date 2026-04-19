using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PortaBox.Infrastructure.Identity;

namespace PortaBox.Api.IntegrationTests.Fixtures;

/// <summary>
/// Composite fixture that starts Postgres + MinIO + MailHog via Testcontainers and provides a
/// <see cref="WebApplicationFactory{TEntryPoint}"/> with the full API pipeline. Use this fixture
/// for E2E tests that exercise the HTTP layer end-to-end.
///
/// xUnit collection: <see cref="AppFactoryCollection"/>
/// </summary>
public sealed class AppFactoryFixture : IAsyncLifetime
{
    private const string OperatorEmail = "operator@portabox.dev";
    private const string OperatorPassword = "PortaBox123!";

    private readonly PostgresDatabaseFixture _postgres = new();
    private readonly MinioFixture _minio = new();
    private readonly MailHogFixture _mailhog = new();

    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.InitializeAsync(),
            _minio.InitializeAsync(),
            _mailhog.InitializeAsync());

        _factory = CreateFactory();

        // Force startup (seed runs during app startup in Development)
        using var client = _factory.CreateClient();
        using var _ = await client.GetAsync("/health/live");
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
        await _minio.DisposeAsync();
        await _mailhog.DisposeAsync();
    }

    /// <summary>Exposes the application's root service provider for test-level scope creation.</summary>
    public IServiceProvider Services => _factory!.Services;

    /// <summary>Exposes the Postgres connection string for isolated factory creation in tests.</summary>
    public string PostgresConnectionString => _postgres.ConnectionString;

    /// <summary>Exposes the MinIO endpoint for isolated factory creation in tests.</summary>
    public string MinioEndpoint => _minio.Endpoint;

    /// <summary>Exposes the MinIO bucket name for isolated factory creation in tests.</summary>
    public string MinioBucketName => _minio.BucketName;

    /// <summary>Creates an unauthenticated HTTP client.</summary>
    public HttpClient CreateClient() => _factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    /// <summary>
    /// Creates an authenticated HTTP client logged in with the operator credentials seeded by
    /// <c>IdentitySeeder</c> during Development startup.
    /// </summary>
    public async Task<HttpClient> CreateOperatorClientAsync()
    {
        var client = CreateClient();
        await LoginAsync(client, OperatorEmail, OperatorPassword);
        return client;
    }

    /// <summary>
    /// Resets all database state between tests and re-seeds the Identity baseline (roles +
    /// development operator) because Respawn clears the asp_net_* tables.
    /// </summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _postgres.ResetAsync(cancellationToken);

        // Re-seed because Respawn clears asp_net_* tables.
        await _factory!.Services.ApplyIdentityMigrationsAndSeedAsync(cancellationToken);
    }

    /// <summary>
    /// Clears all captured MailHog messages. Call between tests to isolate email state.
    /// </summary>
    public async Task ClearMailHogAsync(CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri($"http://{_mailhog.Hostname}:{_mailhog.ApiMappedPort}")
        };

        await client.DeleteAsync("/api/v1/messages", cancellationToken);
    }

    /// <summary>Returns raw JSON payload from the MailHog API.</summary>
    public Task<string> GetMailHogPayloadAsync(CancellationToken cancellationToken = default) =>
        _mailhog.GetMessagesPayloadAsync(cancellationToken);

    /// <summary>
    /// Reads all captured emails from MailHog and extracts magic link tokens
    /// (the <c>token=</c> query param value) from the body.
    /// Handles quoted-printable and base64 MIME part encoding.
    /// </summary>
    public async Task<IReadOnlyList<string>> ExtractMagicLinkTokensAsync(CancellationToken cancellationToken = default)
    {
        var payload = await GetMailHogPayloadAsync(cancellationToken);
        var doc = JsonDocument.Parse(payload);
        var tokens = new List<string>();

        foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
        {
            foreach (var body in GetDecodedMimeBodies(item))
            {
                var match = MagicLinkTokenPattern.Match(body);
                if (match.Success)
                {
                    tokens.Add(match.Groups[1].Value);
                    break;
                }
            }
        }

        return tokens;
    }

    /// <summary>Extracts the most recent magic link token from MailHog captures.</summary>
    public async Task<string> GetLatestMagicLinkTokenAsync(CancellationToken cancellationToken = default)
    {
        // Poll briefly — MailHog indexes messages asynchronously after SMTP delivery.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(400, cancellationToken);

            var tokens = await ExtractMagicLinkTokensAsync(cancellationToken);
            if (tokens.Count > 0)
                return tokens[^1];
        }

        throw new InvalidOperationException("No magic link token found in MailHog. Did the email get sent?");
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Yields decoded text bodies from a MailHog v2 message element.
    /// Handles QP and base64 Content-Transfer-Encoding in MIME parts.
    /// </summary>
    private static IEnumerable<string> GetDecodedMimeBodies(JsonElement item)
    {
        if (!item.TryGetProperty("Content", out var content))
            yield break;

        // Prefer MIME parts (text/plain and text/html each decoded separately)
        if (content.TryGetProperty("MIME", out var mime) &&
            mime.ValueKind == JsonValueKind.Object &&
            mime.TryGetProperty("Parts", out var parts) &&
            parts.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in parts.EnumerateArray())
            {
                if (!part.TryGetProperty("Body", out var bodyProp))
                    continue;

                var rawBody = bodyProp.GetString() ?? string.Empty;
                var encoding = GetTransferEncoding(part);
                yield return DecodeTransferEncoding(rawBody, encoding);
            }
            yield break;
        }

        // Fallback: top-level body (raw multipart — search with QP decoding)
        if (content.TryGetProperty("Body", out var topBody))
            yield return DecodeTransferEncoding(topBody.GetString() ?? string.Empty, "quoted-printable");
    }

    private static string GetTransferEncoding(JsonElement part)
    {
        if (part.TryGetProperty("Headers", out var headers) &&
            headers.TryGetProperty("Content-Transfer-Encoding", out var cteArr) &&
            cteArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in cteArr.EnumerateArray())
                return el.GetString()?.ToLowerInvariant() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string DecodeTransferEncoding(string body, string encoding) => encoding switch
    {
        "quoted-printable" => DecodeQuotedPrintable(body),
        "base64" => DecodeBase64Body(body),
        _ => body
    };

    private static string DecodeQuotedPrintable(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = Regex.Replace(text, @"=\r?\n", string.Empty);  // soft line breaks
        return Regex.Replace(text, @"=([0-9A-Fa-f]{2})",
            m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
    }

    private static string DecodeBase64Body(string body)
    {
        try
        {
            var clean = Regex.Replace(body, @"\s", string.Empty);
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(clean));
        }
        catch
        {
            return body;
        }
    }

    /// <summary>
    /// Creates an isolated <see cref="WebApplicationFactory{Program}"/> that shares the same
    /// Postgres + MinIO containers as this fixture but uses a custom Data Protection keys path.
    /// Useful for testing cookie persistence across factory restarts.
    /// </summary>
    public WebApplicationFactory<Program> CreateIsolatedFactoryWithDataProtection(string keysPath)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings__Postgres"] = _postgres.ConnectionString,
            ["Persistence__ApplyMigrationsOnStartup"] = "true",
            ["Storage__Provider"] = "Minio",
            ["Storage__Endpoint"] = _minio.Endpoint,
            ["Storage__BucketName"] = _minio.BucketName,
            ["Storage__AccessKey"] = "minioadmin",
            ["Storage__SecretKey"] = "minioadmin",
            ["Storage__UseSsl"] = "false",
            ["Storage__ForcePathStyle"] = "true",
            ["Storage__Region"] = "us-east-1",
            ["Email__Host"] = _mailhog.Hostname,
            ["Email__Port"] = _mailhog.SmtpMappedPort.ToString(),
            ["Email__UseStartTls"] = "false",
            ["DomainEvents__Publisher__Enabled"] = "false",
            ["CondominioMagicLink__SindicoAppBaseUrl"] = "https://sindico.portabox.test",
            ["DataProtection__KeysPath"] = keysPath,
        };
        return new EnvVarWebApplicationFactory("Development", settings);
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings__Postgres"] = _postgres.ConnectionString,
            ["Persistence__ApplyMigrationsOnStartup"] = "true",
            ["Storage__Provider"] = "Minio",
            ["Storage__Endpoint"] = _minio.Endpoint,
            ["Storage__BucketName"] = _minio.BucketName,
            ["Storage__AccessKey"] = "minioadmin",
            ["Storage__SecretKey"] = "minioadmin",
            ["Storage__UseSsl"] = "false",
            ["Storage__ForcePathStyle"] = "true",
            ["Storage__Region"] = "us-east-1",
            ["Email__Host"] = _mailhog.Hostname,
            ["Email__Port"] = _mailhog.SmtpMappedPort.ToString(),
            ["Email__UseStartTls"] = "false",
            ["DomainEvents__Publisher__Enabled"] = "false",
            ["CondominioMagicLink__SindicoAppBaseUrl"] = "https://sindico.portabox.test",
            ["RateLimiting__Auth__MaxRequests"] = "1000"
        };

        return new EnvVarWebApplicationFactory("Development", settings);
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
    }

    // Matches: password-setup?token=<TOKEN> inside HTML (supports both raw and encoded = in URL)
    private static readonly Regex MagicLinkTokenPattern =
        new(@"password-setup\?token=([A-Za-z0-9%_\-]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ─── inner factory ────────────────────────────────────────────────────────

    private sealed class EnvVarWebApplicationFactory(
        string environment,
        IReadOnlyDictionary<string, string?> settings)
        : WebApplicationFactory<Program>
    {
        private readonly Dictionary<string, string?> _previousValues = [];

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            foreach (var pair in settings)
            {
                _previousValues[pair.Key] = Environment.GetEnvironmentVariable(pair.Key);
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }

            builder.UseEnvironment(environment);
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

[CollectionDefinition(nameof(AppFactoryCollection))]
public sealed class AppFactoryCollection : ICollectionFixture<AppFactoryFixture>;
