namespace PortaBox.Infrastructure.Email;

public sealed class EmailOutboxEntry
{
    private EmailOutboxEntry()
    {
    }

    public Guid Id { get; private set; }

    public string ToAddress { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string HtmlBody { get; private set; } = string.Empty;

    public string? TextBody { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public static EmailOutboxEntry Create(
        string toAddress,
        string subject,
        string htmlBody,
        string? textBody,
        int attempts,
        DateTimeOffset nextAttemptAt,
        string? lastError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);
        ArgumentOutOfRangeException.ThrowIfNegative(attempts);

        return new EmailOutboxEntry
        {
            Id = Guid.NewGuid(),
            ToAddress = toAddress.Trim(),
            Subject = subject.Trim(),
            HtmlBody = htmlBody,
            TextBody = textBody,
            Attempts = attempts,
            NextAttemptAt = nextAttemptAt,
            LastError = lastError
        };
    }

    public void MarkAsSent(DateTimeOffset sentAt)
    {
        SentAt = sentAt;
        LastError = null;
    }

    public void RecordFailure(int attemptsDelta, DateTimeOffset nextAttemptAt, string? lastError)
    {
        if (attemptsDelta <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptsDelta));
        }

        Attempts += attemptsDelta;
        NextAttemptAt = nextAttemptAt;
        LastError = lastError;
    }
}
