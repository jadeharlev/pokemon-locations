using System.Net;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class GymsControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly RedisFixture redisFixture;

    public GymsControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
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

    private const string GymsJson =
        """[{"gymId":1,"buildingId":5,"locationId":2,"locationName":"Pewter City","buildingName":"Pewter Gym","gymType":"Rock","badgeName":"boulder","gymLeader":"Brock","gymOrder":1}]""";

    [Fact]
    public async Task GetAllReturns401WithoutAuth() {
        var apiClient = Substitute.For<IPokemonLocationsApiClient>();
        var factory = CreateFactory(apiClient);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/gyms");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllProxiesApiResponse() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = Substitute.For<IPokemonLocationsApiClient>();
        apiClient.GetWithStatusAsync("/gyms").Returns(new ApiResponse(200, GymsJson));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/gyms");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal("Brock", body[0].GetProperty("gymLeader").GetString());
    }
}
