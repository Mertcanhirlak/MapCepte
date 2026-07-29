using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Transport.IntegrationTests;

public sealed class SystemEndpointsTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SystemEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LiveHealthReturnsOkWithoutDatabase()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SystemInfoDescribesFoundationRuntime()
    {
        var response = await _client.GetFromJsonAsync<SystemInfo>("/api/system");

        Assert.NotNull(response);
        Assert.Equal("MapCepte Transport API", response.Name);
        Assert.Equal(".NET 10", response.Runtime);
        Assert.Equal("IdentityAuthorization", response.Phase);
    }

    private sealed record SystemInfo(string Name, string Runtime, string Phase);
}
