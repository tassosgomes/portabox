namespace PortaBox.Application.Abstractions.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Smtp";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 25;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "no-reply@portabox.dev";

    public bool UseStartTls { get; set; } = true;
}
