namespace PortaBox.Infrastructure.Email;

public static class EmailOutboxPolicy
{
    public const int RetryAttempts = 3;

    public static TimeSpan ComputeNextDelay(int totalAttempts)
    {
        if (totalAttempts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAttempts));
        }

        var exponent = Math.Min(totalAttempts, 8);
        var minutes = Math.Max(1, (int)Math.Pow(2, exponent));
        return TimeSpan.FromMinutes(minutes);
    }
}
