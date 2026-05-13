using System.Net;
using System.Text;
using System.Text.Json;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("WebServer4")]
public class StatsControllerTests {
    private readonly WebServerFixture fixture;

    public StatsControllerTests(WebServerFixture fixture) {
        this.fixture = fixture;
    }

    private PokemonLocationsWebServerFactory CreateFactory(IPokemonLocationsApiClient? apiClient = null) {
        var client = apiClient ?? Substitute.For<IPokemonLocationsApiClient>();
        fixture.Factory.Overrides.Current = new TestServiceOverrides { ApiClient = client };
        return fixture.Factory;
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
        await ResetUsersAsync(fixture.PostgresConnectionString);
        await SeedUserAsync(fixture.PostgresConnectionString, "red@example.com", "pikachu123", "Red");
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
        await ResetUsersAsync(fixture.PostgresConnectionString);
        await SeedUserAsync(fixture.PostgresConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        // Earn 2 badges
        await client.PutAsync("/api/me/badges/boulder", content: null);
        await client.PutAsync("/api/me/badges/cascade", content: null);
        // Visit 3 buildings across 2 locations: location 1 has buildings 10 & 11, location 2 has building 20.
        // locationsVisited is derived from distinct location_ids in user_visited_buildings.
        await client.PutAsync("/api/me/visited/buildings/1/10", content: null);
        await client.PutAsync("/api/me/visited/buildings/1/11", content: null);
        await client.PutAsync("/api/me/visited/buildings/2/20", content: null);

        var response = await client.GetAsync("/api/me/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(2, body.GetProperty("gymsComplete").GetInt32());
        Assert.Equal(2, body.GetProperty("locationsVisited").GetInt32());
        Assert.Equal(3, body.GetProperty("buildingsVisited").GetInt32());
    }
}
