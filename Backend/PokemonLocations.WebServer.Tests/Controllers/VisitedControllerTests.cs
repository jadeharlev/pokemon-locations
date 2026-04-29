using System.Net;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class VisitedControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly RedisFixture redisFixture;

    public VisitedControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        this.postgresFixture = postgresFixture;
        this.redisFixture = redisFixture;
    }

    private PokemonLocationsWebServerFactory CreateFactory(IPokemonLocationsApiClient apiClient) =>
        new(postgresFixture.ConnectionString, redisFixture.ConnectionString) {
            ApiClient = apiClient
        };

    private static IPokemonLocationsApiClient ApiClientThatAcceptsEverything() {
        var client = Substitute.For<IPokemonLocationsApiClient>();
        client.ExistsAsync(Arg.Any<string>()).Returns(true);
        return client;
    }

    [Fact]
    public async Task PutLocationReturns401WithoutBasicHeader() {
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = factory.CreateClient();

        var response = await client.PutAsync("/api/me/visited/locations/1", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLocationReturns401WithoutBasicHeader() {
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/me/visited/locations/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutBuildingReturns401WithoutBasicHeader() {
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = factory.CreateClient();

        var response = await client.PutAsync("/api/me/visited/buildings/1/2", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBuildingReturns401WithoutBasicHeader() {
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/me/visited/buildings/1/2");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutLocationReturns204ForKnownLocation() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = Substitute.For<IPokemonLocationsApiClient>();
        apiClient.ExistsAsync("/locations/42").Returns(true);
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.PutAsync("/api/me/visited/locations/42", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await apiClient.Received(1).ExistsAsync("/locations/42");
    }

    [Fact]
    public async Task PutLocationReturns404ForUnknownLocation() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = Substitute.For<IPokemonLocationsApiClient>();
        apiClient.ExistsAsync("/locations/999").Returns(false);
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.PutAsync("/api/me/visited/locations/999", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("not_found", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PutLocationIsIdempotent() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        await client.PutAsync("/api/me/visited/locations/1", content: null);
        var second = await client.PutAsync("/api/me/visited/locations/1", content: null);

        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task DeleteLocationReturns204AndIsIdempotent() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");
        await client.PutAsync("/api/me/visited/locations/1", content: null);

        var first = await client.DeleteAsync("/api/me/visited/locations/1");
        var second = await client.DeleteAsync("/api/me/visited/locations/1");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task DeleteLocationDoesNotCallApi() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = Substitute.For<IPokemonLocationsApiClient>();
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        await client.DeleteAsync("/api/me/visited/locations/1");

        await apiClient.DidNotReceive().ExistsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task PutBuildingReturns204ForKnownBuilding() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = Substitute.For<IPokemonLocationsApiClient>();
        apiClient.ExistsAsync("/locations/3/buildings/7").Returns(true);
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.PutAsync("/api/me/visited/buildings/3/7", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await apiClient.Received(1).ExistsAsync("/locations/3/buildings/7");
    }

    [Fact]
    public async Task PutBuildingReturns404ForUnknownBuilding() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = Substitute.For<IPokemonLocationsApiClient>();
        apiClient.ExistsAsync(Arg.Any<string>()).Returns(false);
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.PutAsync("/api/me/visited/buildings/3/999", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutBuildingIsIdempotent() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        await client.PutAsync("/api/me/visited/buildings/1/2", content: null);
        var second = await client.PutAsync("/api/me/visited/buildings/1/2", content: null);

        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task DeleteBuildingReturns204AndIsIdempotent() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");
        await client.PutAsync("/api/me/visited/buildings/1/2", content: null);

        var first = await client.DeleteAsync("/api/me/visited/buildings/1/2");
        var second = await client.DeleteAsync("/api/me/visited/buildings/1/2");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    private static HttpClient AuthorizedClient(
        PokemonLocationsWebServerFactory factory, string email, string password) {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader(email, password);
        return client;
    }
}
