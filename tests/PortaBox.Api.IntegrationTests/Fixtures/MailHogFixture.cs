using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace PortaBox.Api.IntegrationTests.Fixtures;

public sealed class MailHogFixture : IAsyncLifetime
{
    private const ushort SmtpPort = 1025;
    private const ushort ApiPort = 8025;

    private readonly IContainer _container = new ContainerBuilder("mailhog/mailhog:v1.0.1")
        .WithPortBinding(SmtpPort, true)
        .WithPortBinding(ApiPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(ApiPort).ForPath("/api/v2/messages")))
        .Build();

    public string Hostname => _container.Hostname;

    public int SmtpMappedPort => _container.GetMappedPublicPort(SmtpPort);

    public int ApiMappedPort => _container.GetMappedPublicPort(ApiPort);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task<string> GetMessagesPayloadAsync(CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri($"http://{Hostname}:{ApiMappedPort}")
        };

        return await client.GetStringAsync("/api/v2/messages", cancellationToken);
    }
}
