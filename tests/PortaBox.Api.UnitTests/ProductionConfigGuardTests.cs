using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PortaBox.Api.Infrastructure;

namespace PortaBox.Api.UnitTests;

public sealed class ProductionConfigGuardTests
{
    // ─── ValidateEmailTlsRequired ────────────────────────────────────────────

    [Fact]
    public void ValidateEmailTlsRequired_Production_UseStartTlsFalse_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Email:UseStartTls"] = "false" })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigGuard.ValidateEmailTlsRequired(config, FakeEnv("Production")));

        Assert.Contains("UseStartTls", ex.Message);
    }

    [Fact]
    public void ValidateEmailTlsRequired_Production_UseStartTlsTrue_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Email:UseStartTls"] = "true" })
            .Build();

        ProductionConfigGuard.ValidateEmailTlsRequired(config, FakeEnv("Production"));
    }

    [Fact]
    public void ValidateEmailTlsRequired_Development_UseStartTlsFalse_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Email:UseStartTls"] = "false" })
            .Build();

        // Must not throw outside Production
        ProductionConfigGuard.ValidateEmailTlsRequired(config, FakeEnv("Development"));
    }

    // ─── ValidateSecretsNotInJsonFiles ───────────────────────────────────────

    [Fact]
    public void ValidateSecretsNotInJsonFiles_Production_SecretInJsonFile_Throws()
    {
        var jsonContent = """{"Storage":{"AccessKey":"real-secret-key"}}""";
        var configRoot = BuildConfigRootFromJsonContent(jsonContent);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigGuard.ValidateSecretsNotInJsonFiles(configRoot, FakeEnv("Production")));

        Assert.Contains("Storage:AccessKey", ex.Message);
    }

    [Fact]
    public void ValidateSecretsNotInJsonFiles_Production_NoSecretsInJson_DoesNotThrow()
    {
        var jsonContent = """{"Email":{"UseStartTls":true}}""";
        var configRoot = BuildConfigRootFromJsonContent(jsonContent);

        ProductionConfigGuard.ValidateSecretsNotInJsonFiles(configRoot, FakeEnv("Production"));
    }

    [Fact]
    public void ValidateSecretsNotInJsonFiles_Development_SecretInJsonFile_DoesNotThrow()
    {
        var jsonContent = """{"Storage":{"AccessKey":"minioadmin"}}""";
        var configRoot = BuildConfigRootFromJsonContent(jsonContent);

        // Must not throw outside Production (dev can have secrets in appsettings.Development.json)
        ProductionConfigGuard.ValidateSecretsNotInJsonFiles(configRoot, FakeEnv("Development"));
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static IConfigurationRoot BuildConfigRootFromJsonContent(string json)
    {
        var tempFile = Path.GetTempFileName() + ".json";
        File.WriteAllText(tempFile, json);
        try
        {
            return new ConfigurationBuilder()
                .AddJsonFile(tempFile, optional: false, reloadOnChange: false)
                .Build();
        }
        finally
        {
            // Cleanup handled by OS; file may be locked briefly after Build()
        }
    }

    private static IHostEnvironment FakeEnv(string name) => new FakeHostEnvironment(name);

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "PortaBox.Tests";
        public string EnvironmentName { get; set; } = environmentName;
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
