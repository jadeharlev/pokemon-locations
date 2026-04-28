using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Npgsql;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests;

[Collection("PostgresAndRedis")]
public class HealthCheckTests {
    private readonly PostgresFixture postgresFixture;
    private readonly PokemonLocationsWebServerFactory factory;

    public HealthCheckTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        this.postgresFixture = postgresFixture;
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

    [Fact]
    public async Task ApiMeReturnsUnauthorizedWithoutBasicAuthHeader() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(
            response.Headers.WwwAuthenticate,
            h => h.Scheme == "Basic");
    }

    [Fact]
    public async Task ApiMeReturnsUnauthorizedForMalformedAuthHeader() {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.TryAddWithoutValidation("Authorization", "Basic not-base64!!!");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiMeReturnsUnauthorizedForUnknownUser() {
        await ResetUsersAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader("ghost@example.com", "anything");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiMeReturnsUnauthorizedForWrongPassword() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader("red@example.com", "WRONG");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiMeReturnsOkForValidBasicAuthHeader() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader("red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static AuthenticationHeaderValue BasicHeader(string email, string password) {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{password}"));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    private async Task SeedUserAsync(string email, string password, string displayName) {
        var hasher = new PasswordHasher();
        var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        var repository = new UserRepository(dataSource);
        await repository.CreateAsync(email, hasher.HashPassword(password), displayName);
    }

    private async Task ResetUsersAsync() {
        await using var connection = new NpgsqlConnection(postgresFixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM users";
        await command.ExecuteNonQueryAsync();
    }
}
