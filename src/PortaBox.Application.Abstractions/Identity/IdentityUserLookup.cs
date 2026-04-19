namespace PortaBox.Application.Abstractions.Identity;

public sealed record IdentityUserLookup(
    Guid UserId,
    string Email,
    bool HasPassword,
    Guid? SindicoTenantId);
