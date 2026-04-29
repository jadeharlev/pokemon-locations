using System.Net;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class BuildingsControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly RedisFixture redisFixture;

    public BuildingsControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        this.postgresFixture = postgresFixture;
        this.redisFixture = redisFixture;
    }

    private PokemonLocationsWebServerFactory CreateFactory(IPokemonLocationsApiClient apiClient) =>
        new(postgresFixture.ConnectionString, redisFixture.ConnectionString) {
            ApiClient = apiClient
        };

    private static HttpClient AuthorizedClient(
        PokemonLocationsWebServerFactory factory, string email, string password) {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader(email, password);
        return client;
    }

    private static IPokemonLocationsApiClient CreateApiClient() {
        var client = Substitute.For<IPokemonLocationsApiClient>();
        client.ExistsAsync(Arg.Any<string>()).Returns(true);
        return client;
    }

    private const string TwoBuildingsJson =
        """[{"buildingId":10,"locationId":1,"name":"Player's House","buildingType":3,"description":null,"landmarkDescription":null,"gym":null},{"buildingId":11,"locationId":1,"name":"Oak's Lab","buildingType":5,"description":"Professor Oak's laboratory.","landmarkDescription":null,"gym":null}]""";

    private const string SingleBuildingJson =
        """{"buildingId":10,"locationId":1,"name":"Player's House","buildingType":3,"description":null,"landmarkDescription":null,"gym":null}""";

    // --- 401 tests ---

    [Fact]
    public async Task GetAllReturns401WithoutAuth() {
        var factory = CreateFactory(CreateApiClient());
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/locations/1/buildings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByIdReturns401WithoutAuth() {
        var factory = CreateFactory(CreateApiClient());
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/locations/1/buildings/10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- GET /api/locations/{locationId}/buildings ---

    [Fact]
    public async Task GetAllProxiesAndMergesVisitedFlag() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/1/buildings").Returns(new ApiResponse(200, TwoBuildingsJson));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        // Mark building 10 as visited
        await client.PutAsync("/api/me/visited/buildings/1/10", content: null);

        var response = await client.GetAsync("/api/locations/1/buildings");
        var body = await ReadJsonAsync(response);

        Assert.Equal(2, body.GetArrayLength());
        Assert.True(body[0].GetProperty("visited").GetBoolean());
        Assert.False(body[1].GetProperty("visited").GetBoolean());
    }

    [Fact]
    public async Task GetAllPropagatesApi404() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/999/buildings").Returns(new ApiResponse(404, null));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/locations/999/buildings");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- GET /api/locations/{locationId}/buildings/{buildingId} ---

    [Fact]
    public async Task GetByIdProxiesWithoutMerging() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/1/buildings/10").Returns(new ApiResponse(200, SingleBuildingJson));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/locations/1/buildings/10");
        var body = await ReadJsonAsync(response);

        Assert.Equal("Player's House", body.GetProperty("name").GetString());
        // No "visited" property on single building GET — pure proxy
        Assert.False(body.TryGetProperty("visited", out _));
    }

    [Fact]
    public async Task GetByIdPropagatesApi404() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/1/buildings/999").Returns(new ApiResponse(404, null));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/locations/1/buildings/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
