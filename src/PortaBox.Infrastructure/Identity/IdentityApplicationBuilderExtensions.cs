using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PortaBox.Infrastructure.Identity;

public static class IdentityApplicationBuilderExtensions
{
    public static async Task ApplyIdentityMigrationsAndSeedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Persistence.AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.RunAsync(cancellationToken);
    }
}
