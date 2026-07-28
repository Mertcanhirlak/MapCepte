namespace Transport.UnitTests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void DomainAssemblyHasExpectedName()
    {
        var assemblyName = typeof(Domain.AssemblyReference).Assembly.GetName().Name;

        Assert.Equal("Transport.Domain", assemblyName);
    }

    [Fact]
    public void ApplicationAssemblyHasExpectedName()
    {
        var assemblyName = typeof(Application.AssemblyReference).Assembly.GetName().Name;

        Assert.Equal("Transport.Application", assemblyName);
    }
}
