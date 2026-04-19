using Microsoft.Extensions.Configuration.Json;

namespace PortaBox.Api.Infrastructure;

public static class ProductionConfigGuard
{
    private static readonly string[] SecretKeysProhibitedInJsonConfig =
    [
        "Storage:AccessKey",
        "Storage:SecretKey",
        "Email:Username",
        "Email:Password",
        "Identity:DevelopmentOperator:Password",
    ];

    public static void ValidateEmailTlsRequired(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;

        var useStartTls = configuration.GetValue<bool>("Email:UseStartTls", false);
        if (!useStartTls)
        {
            throw new InvalidOperationException(
                "Email:UseStartTls must be true in Production. " +
                "Set the Email__UseStartTls environment variable to 'true'.");
        }
    }

    public static void ValidateSecretsNotInJsonFiles(IConfigurationRoot configRoot, IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;

        foreach (var key in SecretKeysProhibitedInJsonConfig)
        {
            foreach (var provider in configRoot.Providers.OfType<JsonConfigurationProvider>())
            {
                if (provider.TryGet(key, out var value) && !string.IsNullOrEmpty(value))
                {
                    throw new InvalidOperationException(
                        $"Secret key '{key}' must not be defined in a JSON configuration file. " +
                        "Use environment variables to prevent accidental exposure in source control.");
                }
            }
        }
    }
}
