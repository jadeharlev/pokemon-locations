using System.Net;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests;

[Collection("WebServer4")]
public class HealthCheckTests {
    private readonly PokemonLocationsWebServerFactory factory;

    public HealthCheckTests(WebServerFixture fixture) {
        factory = fixture.Factory;
    }

    [Fact]
    public async Task HealthDbReturnsOkAnonymously() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/db");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
