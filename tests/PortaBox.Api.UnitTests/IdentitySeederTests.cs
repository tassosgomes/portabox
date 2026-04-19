using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.UnitTests;

public class IdentitySeederTests
{
    [Fact]
    public async Task RunAsync_ShouldCreateOperatorRole_WhenItDoesNotExist()
    {
        await using var testContext = await IdentityTestContext.CreateAsync("Development");

        await testContext.Seeder.RunAsync();

        Assert.True(await testContext.RoleManager.RoleExistsAsync(IdentityRoles.Operator));
    }

    [Fact]
    public async Task RunAsync_ShouldBeIdempotent_WhenInvokedTwice()
    {
        await using var testContext = await IdentityTestContext.CreateAsync("Development");

        await testContext.Seeder.RunAsync();
        await testContext.Seeder.RunAsync();

        Assert.Equal(2, await testContext.DbContext.Roles.CountAsync());
        Assert.Equal(1, await testContext.DbContext.Users.CountAsync());
        Assert.Equal(1, await testContext.DbContext.UserRoles.CountAsync());
    }

    [Fact]
    public async Task RunAsync_ShouldCreateDevelopmentOperatorUsingConfiguredPassword_InDevelopment()
    {
        const string password = "CustomPass123!";
        await using var testContext = await IdentityTestContext.CreateAsync(
            "Development",
            options =>
            {
                options.DevelopmentOperator.Password = password;
            });

        await testContext.Seeder.RunAsync();

        var user = await testContext.UserManager.FindByEmailAsync("operator@portabox.dev");

        Assert.NotNull(user);
        Assert.True(await testContext.UserManager.CheckPasswordAsync(user, password));
        Assert.True(await testContext.UserManager.IsInRoleAsync(user, IdentityRoles.Operator));
    }

    [Fact]
    public async Task RunAsync_ShouldNotCreateDevelopmentOperator_InProduction()
    {
        await using var testContext = await IdentityTestContext.CreateAsync("Production");

        await testContext.Seeder.RunAsync();

        Assert.Null(await testContext.UserManager.FindByEmailAsync("operator@portabox.dev"));
        Assert.Equal(2, await testContext.DbContext.Roles.CountAsync());
    }

    private sealed class IdentityTestContext : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private IdentityTestContext(
            ServiceProvider serviceProvider,
            AppDbContext dbContext,
            IdentitySeeder seeder,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager)
        {
            _serviceProvider = serviceProvider;
            DbContext = dbContext;
            Seeder = seeder;
            UserManager = userManager;
            RoleManager = roleManager;
        }

        public AppDbContext DbContext { get; }

        public IdentitySeeder Seeder { get; }

        public UserManager<AppUser> UserManager { get; }

        public RoleManager<AppRole> RoleManager { get; }

        public static async Task<IdentityTestContext> CreateAsync(
            string environmentName,
            Action<IdentityConfiguration>? configure = null)
        {
            var services = new ServiceCollection();
            var identityConfiguration = new IdentityConfiguration();
            configure?.Invoke(identityConfiguration);

            services.AddLogging();
            services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(environmentName));
            services.AddSingleton<IOptions<IdentityConfiguration>>(Microsoft.Extensions.Options.Options.Create(identityConfiguration));
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"identity-seeder-{Guid.NewGuid():N}"));
            services
                .AddIdentityCore<AppUser>(options =>
                {
                    options.Password.RequireDigit = identityConfiguration.Password.RequireDigit;
                    options.Password.RequireLowercase = identityConfiguration.Password.RequireLowercase;
                    options.Password.RequireUppercase = identityConfiguration.Password.RequireUppercase;
                    options.Password.RequireNonAlphanumeric = identityConfiguration.Password.RequireNonAlphanumeric;
                    options.Password.RequiredLength = identityConfiguration.Password.RequiredLength;
                    options.Password.RequiredUniqueChars = identityConfiguration.Password.RequiredUniqueChars;
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<AppRole>()
                .AddEntityFrameworkStores<AppDbContext>();
            services.AddScoped<IdentitySeeder>();

            var serviceProvider = services.BuildServiceProvider();
            var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            return new IdentityTestContext(
                serviceProvider,
                dbContext,
                scope.ServiceProvider.GetRequiredService<IdentitySeeder>(),
                scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(),
                scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>());
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _serviceProvider.DisposeAsync();
        }
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "PortaBox.Tests";

        public string EnvironmentName { get; set; } = environmentName;

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
