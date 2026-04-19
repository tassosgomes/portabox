using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace PortaBox.Api.UnitTests;

internal static class TestHostProblemDetailsFactory
{
    public static ProblemDetailsFactory Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        services.AddProblemDetails();

        return services.BuildServiceProvider().GetRequiredService<ProblemDetailsFactory>();
    }
}
