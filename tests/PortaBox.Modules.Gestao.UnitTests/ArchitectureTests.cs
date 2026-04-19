namespace PortaBox.Modules.Gestao.UnitTests;

public class ArchitectureTests
{
    [Fact]
    public void ModuleAssembly_ShouldBeDiscoverable()
    {
        var assembly = typeof(PortaBox.Modules.Gestao.DependencyInjection).Assembly;

        Assert.Equal("PortaBox.Modules.Gestao", assembly.GetName().Name);
    }

    [Fact]
    public void ModuleProject_ShouldReferenceAllowedProjects()
    {
        var projectFile = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../src/PortaBox.Modules.Gestao/PortaBox.Modules.Gestao.csproj"));

        var projectContents = File.ReadAllText(projectFile);

        Assert.Contains("PortaBox.Application.Abstractions.csproj", projectContents);
        Assert.Contains("PortaBox.Domain.csproj", projectContents);
        Assert.DoesNotContain("PortaBox.Infrastructure.csproj", projectContents);
        Assert.DoesNotContain("PortaBox.Api.csproj", projectContents);
    }
}
