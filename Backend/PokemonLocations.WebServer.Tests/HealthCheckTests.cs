using System.Net;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests;

[Collection("PostgresAndRedis")]
public class HealthCheckTests {
    private readonly PokemonLocationsWebServerFactory factory;

    public HealthCheckTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        factory = new PokemonLocationsWebServerFactory(
            postgresFixture.ConnectionString,
            redisFixture.ConnectionString);
    }

    [Fact]
    public async Task HealthDbReturnsOkAnonymously() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/db");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
