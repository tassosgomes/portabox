namespace PortaBox.Application.Abstractions.Identity;

public sealed record IdentityUserDescriptor(
    Guid UserId,
    string Email,
    string FullName,
    Guid? SindicoTenantId);
