using System.Net;
using System.Text;
using System.Text.Json;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class StatsControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly RedisFixture redisFixture;

    public StatsControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        this.postgresFixture = postgresFixture;
        this.redisFixture = redisFixture;
    }

    private PokemonLocationsWebServerFactory CreateFactory(IPokemonLocationsApiClient? apiClient = null) {
        var client = apiClient ?? Substitute.For<IPokemonLocationsApiClient>();
        return new(postgresFixture.ConnectionString, redisFixture.ConnectionString) {
            ApiClient = client
        };
    }

    private static IPokemonLocationsApiClient ApiClientThatAcceptsEverything() {
        var client = Substitute.For<IPokemonLocationsApiClient>();
        client.ExistsAsync(Arg.Any<string>()).Returns(true);
        return client;
    }

    private static HttpClient AuthorizedClient(
        PokemonLocationsWebServerFactory factory, string email, string password) {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader(email, password);
        return client;
    }

    [Fact]
    public async Task GetReturns401WithoutAuth() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me/stats");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReturnsZerosForNewUser() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory();
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/me/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(0, body.GetProperty("gymsComplete").GetInt32());
        Assert.Equal(0, body.GetProperty("locationsVisited").GetInt32());
        Assert.Equal(0, body.GetProperty("buildingsVisited").GetInt32());
    }

    [Fact]
    public async Task GetReturnsCorrectCounts() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        // Earn 2 badges
        await client.PutAsync("/api/me/badges/boulder", content: null);
        await client.PutAsync("/api/me/badges/cascade", content: null);
        // Visit 1 location
        await client.PutAsync("/api/me/visited/locations/1", content: null);
        // Visit 3 buildings
        await client.PutAsync("/api/me/visited/buildings/1/10", content: null);
        await client.PutAsync("/api/me/visited/buildings/1/11", content: null);
        await client.PutAsync("/api/me/visited/buildings/2/20", content: null);

        var response = await client.GetAsync("/api/me/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(2, body.GetProperty("gymsComplete").GetInt32());
        Assert.Equal(1, body.GetProperty("locationsVisited").GetInt32());
        Assert.Equal(3, body.GetProperty("buildingsVisited").GetInt32());
    }
}
