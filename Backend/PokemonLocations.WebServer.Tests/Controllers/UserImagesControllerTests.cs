using System.Net;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class UserImagesControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly RedisFixture redisFixture;

    public UserImagesControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        this.postgresFixture = postgresFixture;
        this.redisFixture = redisFixture;
    }

    private PokemonLocationsWebServerFactory CreateFactory(IPokemonLocationsApiClient? apiClient = null) =>
        new(postgresFixture.ConnectionString, redisFixture.ConnectionString) {
            ApiClient = apiClient ?? Substitute.For<IPokemonLocationsApiClient>()
        };

    [Fact]
    public async Task PostReturns401WithoutAuth() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        using var content = new MultipartFormDataContent {
            { new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }), "file", "x.png" }
        };
        var response = await client.PostAsync("/api/me/locations/1/images", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReturns401WithoutAuth() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/me/locations/1/images/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReturns401WithoutAuth() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/me/locations/1/images/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
