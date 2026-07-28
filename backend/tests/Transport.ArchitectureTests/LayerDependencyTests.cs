using System.Reflection;

namespace Transport.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void DomainDoesNotReferenceOuterApplicationLayers()
    {
        var references = GetProjectReferences(typeof(Domain.AssemblyReference).Assembly);

        Assert.DoesNotContain("Transport.Application", references);
        Assert.DoesNotContain("Transport.Infrastructure", references);
        Assert.DoesNotContain("Transport.Api", references);
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        var references = GetProjectReferences(typeof(Application.AssemblyReference).Assembly);

        Assert.DoesNotContain("Transport.Infrastructure", references);
        Assert.DoesNotContain("Transport.Api", references);
    }

    private static string[] GetProjectReferences(Assembly assembly) =>
        assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("Transport.", StringComparison.Ordinal))
            .Cast<string>()
            .ToArray();
}
