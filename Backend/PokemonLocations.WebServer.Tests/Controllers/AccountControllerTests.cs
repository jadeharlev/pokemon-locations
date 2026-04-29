using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class AccountControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly PokemonLocationsWebServerFactory factory;

    public AccountControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        this.postgresFixture = postgresFixture;
        factory = new PokemonLocationsWebServerFactory(
            postgresFixture.ConnectionString,
            redisFixture.ConnectionString);
    }

    [Fact]
    public async Task SignupReturns201AndPersistsUser() {
        await ResetUsersAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/account/signup", new {
            email = "red@example.com",
            password = "pikachu123",
            displayName = "Red"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.True(body.GetProperty("userId").GetInt32() > 0);
        Assert.Equal("red@example.com", body.GetProperty("email").GetString());
        Assert.Equal("Red", body.GetProperty("displayName").GetString());
        Assert.Equal("bulbasaur", body.GetProperty("theme").GetString());

        await using var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        var repository = new UserRepository(dataSource);
        var stored = await repository.GetByEmailAsync("red@example.com");
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task SignupReturns409ForDuplicateEmail() {
        await ResetUsersAsync();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/account/signup", new {
            email = "red@example.com",
            password = "pikachu123",
            displayName = "Red"
        });

        var response = await client.PostAsJsonAsync("/account/signup", new {
            email = "red@example.com",
            password = "different-pw",
            displayName = "Red Again"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("email_taken", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task SignupReturns400ForInvalidEmail() {
        await ResetUsersAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/account/signup", new {
            email = "not-an-email",
            password = "pikachu123",
            displayName = "Red"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SignupReturns400ForShortPassword() {
        await ResetUsersAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/account/signup", new {
            email = "red@example.com",
            password = "short",
            displayName = "Red"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SignupReturns400ForMissingDisplayName() {
        await ResetUsersAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/account/signup", new {
            email = "red@example.com",
            password = "pikachu123",
            displayName = "   "
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SignupReturns400ForOversizedDisplayName() {
        await ResetUsersAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/account/signup", new {
            email = "red@example.com",
            password = "pikachu123",
            displayName = new string('R', 51)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NewUserCanAuthenticateImmediately() {
        await ResetUsersAsync();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/account/signup", new {
            email = "red@example.com",
            password = "pikachu123",
            displayName = "Red"
        });

        client.DefaultRequestHeaders.Authorization = BasicHeader("red@example.com", "pikachu123");
        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("red@example.com", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task DeleteAccountReturns204AndRemovesUser() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader("red@example.com", "pikachu123");

        var response = await client.DeleteAsync("/account");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        var repository = new UserRepository(dataSource);
        Assert.Null(await repository.GetByEmailAsync("red@example.com"));
    }

    [Fact]
    public async Task DeleteAccountReturns401WithoutBasicHeader() {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/account");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccountReturns401ForWrongPassword() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader("red@example.com", "WRONG");

        var response = await client.DeleteAsync("/account");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await using var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        var repository = new UserRepository(dataSource);
        Assert.NotNull(await repository.GetByEmailAsync("red@example.com"));
    }

    [Fact]
    public async Task MeReturnsCurrentUserShapeWithoutPasswordHash() {
        await ResetUsersAsync();
        await SeedUserAsync("red@example.com", "pikachu123", "Red");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader("red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password_hash", raw);
        var body = JsonDocument.Parse(raw).RootElement;
        Assert.True(body.GetProperty("userId").GetInt32() > 0);
        Assert.Equal("red@example.com", body.GetProperty("email").GetString());
        Assert.Equal("Red", body.GetProperty("displayName").GetString());
        Assert.Equal("bulbasaur", body.GetProperty("theme").GetString());
    }

    [Fact]
    public async Task MeReturns401WithoutBasicHeader() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static AuthenticationHeaderValue BasicHeader(string email, string password) {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{password}"));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) {
        var raw = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(raw).RootElement;
    }

    private async Task SeedUserAsync(string email, string password, string displayName) {
        var hasher = new PasswordHasher();
        await using var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        var repository = new UserRepository(dataSource);
        await repository.CreateAsync(email, hasher.HashPassword(password), displayName);
    }

    private async Task ResetUsersAsync() {
        await using var connection = new NpgsqlConnection(postgresFixture.ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM users");
    }
}
