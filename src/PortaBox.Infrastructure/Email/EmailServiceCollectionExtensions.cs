using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions.Email;

namespace PortaBox.Infrastructure.Email;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        services.AddSingleton<IOptions<EmailOptions>>(Options.Create(options));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IEmailTemplateRenderer, EmbeddedResourceEmailTemplateRenderer>();
        services.AddScoped<SmtpEmailDispatcher>();
        services.AddScoped<EmailOutboxProcessor>();
        services.AddScoped<IEmailTransport, MailKitEmailTransport>();

        if (ShouldUseFakeSender(options))
        {
            services.AddSingleton<FakeEmailSender>();
            services.AddSingleton<IEmailSender>(serviceProvider => serviceProvider.GetRequiredService<FakeEmailSender>());
            return services;
        }

        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddHostedService<EmailOutboxRetryWorker>();
        return services;
    }

    private static bool ShouldUseFakeSender(EmailOptions options)
    {
        if (string.Equals(options.Provider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var environmentName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        return string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
    }
}
