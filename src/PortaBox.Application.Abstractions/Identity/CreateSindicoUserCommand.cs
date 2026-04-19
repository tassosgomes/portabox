namespace PortaBox.Application.Abstractions.Identity;

public sealed record CreateSindicoUserCommand(
    string Email,
    string FullName,
    Guid TenantId);
