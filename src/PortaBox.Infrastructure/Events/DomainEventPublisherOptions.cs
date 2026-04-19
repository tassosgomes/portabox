namespace PortaBox.Infrastructure.Events;

public sealed class DomainEventPublisherOptions
{
    public const string SectionName = "DomainEvents:Publisher";

    public int BatchSize { get; set; } = 100;

    public bool Enabled { get; set; } = true;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(15);
}
