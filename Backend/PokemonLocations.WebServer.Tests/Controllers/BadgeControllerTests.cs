using System.Net;
using System.Text.Json;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class BadgeControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly PokemonLocationsWebServerFactory factory;

    public BadgeControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        this.postgresFixture = postgresFixture;
        factory = new PokemonLocationsWebServerFactory(
            postgresFixture.ConnectionString,
            redisFixture.ConnectionString);
    }

    [Fact]
    public async Task GetReturns401WithoutBasicHeader() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me/badges");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutReturns401WithoutBasicHeader() {
        var client = factory.CreateClient();

        var response = await client.PutAsync("/api/me/badges/boulder", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReturns401WithoutBasicHeader() {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/me/badges/boulder");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReturnsEmptyArrayForNewUser() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = AuthorizedClient("red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/me/badges");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task PutMarksBadgeAndGetReturnsIt() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = AuthorizedClient("red@example.com", "pikachu123");

        var put = await client.PutAsync("/api/me/badges/boulder", content: null);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var get = await client.GetAsync("/api/me/badges");
        var body = await ReadJsonAsync(get);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal("boulder", body[0].GetString());
    }

    [Fact]
    public async Task PutIsIdempotent() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = AuthorizedClient("red@example.com", "pikachu123");

        await client.PutAsync("/api/me/badges/boulder", content: null);
        var second = await client.PutAsync("/api/me/badges/boulder", content: null);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var get = await client.GetAsync("/api/me/badges");
        var body = await ReadJsonAsync(get);
        Assert.Equal(1, body.GetArrayLength());
    }

    [Fact]
    public async Task DeleteRemovesBadge() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = AuthorizedClient("red@example.com", "pikachu123");
        await client.PutAsync("/api/me/badges/boulder", content: null);

        var delete = await client.DeleteAsync("/api/me/badges/boulder");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await client.GetAsync("/api/me/badges");
        var body = await ReadJsonAsync(get);
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task DeleteIsIdempotent() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = AuthorizedClient("red@example.com", "pikachu123");

        var response = await client.DeleteAsync("/api/me/badges/thunder");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutReturns400ForInvalidBadge() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = AuthorizedClient("red@example.com", "pikachu123");

        var response = await client.PutAsync("/api/me/badges/notreal", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("invalid_badge", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task DeleteReturns400ForInvalidBadge() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = AuthorizedClient("red@example.com", "pikachu123");

        var response = await client.DeleteAsync("/api/me/badges/notreal");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAcceptsBadgeNameCaseInsensitively() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = AuthorizedClient("red@example.com", "pikachu123");

        var response = await client.PutAsync("/api/me/badges/BOULDER", content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var get = await client.GetAsync("/api/me/badges");
        var body = await ReadJsonAsync(get);
        Assert.Equal("boulder", body[0].GetString());
    }

    [Fact]
    public async Task BadgesAreScopedToAuthenticatedUser() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        await SeedUserAsync("blue@example.com", "squirtle456", "Blue");
        var redClient = AuthorizedClient("red@example.com", "pikachu123");
        var blueClient = AuthorizedClient("blue@example.com", "squirtle456");

        await redClient.PutAsync("/api/me/badges/boulder", content: null);
        await blueClient.PutAsync("/api/me/badges/cascade", content: null);

        var redBody = await ReadJsonAsync(await redClient.GetAsync("/api/me/badges"));
        var blueBody = await ReadJsonAsync(await blueClient.GetAsync("/api/me/badges"));

        Assert.Equal(1, redBody.GetArrayLength());
        Assert.Equal("boulder", redBody[0].GetString());
        Assert.Equal(1, blueBody.GetArrayLength());
        Assert.Equal("cascade", blueBody[0].GetString());
    }

    private HttpClient AuthorizedClient(string email, string password) {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader(email, password);
        return client;
    }

    private Task SeedUserAsync(string email, string password, string displayName) =>
        TestHelpers.SeedUserAsync(postgresFixture.ConnectionString, email, password, displayName);

    private Task ResetUsersAsync() =>
        TestHelpers.ResetUsersAsync(postgresFixture.ConnectionString);
}
