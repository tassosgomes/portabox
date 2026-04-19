using Microsoft.AspNetCore.Identity;

namespace PortaBox.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public Guid? SindicoTenantId { get; set; }
}
