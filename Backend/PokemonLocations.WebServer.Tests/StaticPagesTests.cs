using System.Net;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests;

[Collection("PostgresAndRedis")]
public class StaticPagesTests {
    private readonly PokemonLocationsWebServerFactory factory;

    public StaticPagesTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        factory = new PokemonLocationsWebServerFactory(
            postgresFixture.ConnectionString,
            redisFixture.ConnectionString);
    }

    [Fact]
    public async Task SignInPageIsReachableAnonymously() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/signin.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SignUpPageIsReachableAnonymously() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/signup.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AuthScriptIsReachableAnonymously() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/js/auth.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/javascript", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task IndexPageIsReachableAnonymously() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
