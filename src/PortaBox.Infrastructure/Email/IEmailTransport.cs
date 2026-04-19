using PortaBox.Application.Abstractions.Email;

namespace PortaBox.Infrastructure.Email;

public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
