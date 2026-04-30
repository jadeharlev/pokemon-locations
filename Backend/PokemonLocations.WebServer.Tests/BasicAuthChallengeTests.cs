using System.Net;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests;

[Collection("PostgresAndRedis")]
public class BasicAuthChallengeTests {
    private readonly PokemonLocationsWebServerFactory factory;

    public BasicAuthChallengeTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        factory = new PokemonLocationsWebServerFactory(
            postgresFixture.ConnectionString,
            redisFixture.ConnectionString);
    }

    [Fact]
    public async Task UnauthenticatedApiCallReturns401WithoutBasicChallenge() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain(
            response.Headers.WwwAuthenticate,
            h => string.Equals(h.Scheme, "Basic", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UnauthenticatedAccountCallReturns401WithoutBasicChallenge() {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/account");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain(
            response.Headers.WwwAuthenticate,
            h => string.Equals(h.Scheme, "Basic", StringComparison.OrdinalIgnoreCase));
    }
}
