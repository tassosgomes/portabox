using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Commands.PasswordSetup;

public sealed record PasswordSetupCommand(
    string RawToken,
    string Password,
    string? IpAddress) : ICommand<PasswordSetupResult>;
