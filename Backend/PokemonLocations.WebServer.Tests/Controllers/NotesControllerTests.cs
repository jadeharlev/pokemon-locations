using System.Net;
using System.Text;
using System.Text.Json;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class NotesControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly RedisFixture redisFixture;

    public NotesControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
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

    private static StringContent JsonBody(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    // --- 401 tests ---

    [Fact]
    public async Task GetReturns401WithoutBasicHeader() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me/notes/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutReturns401WithoutBasicHeader() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PutAsync("/api/me/notes/1", JsonBody(new { noteText = "hi" }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReturns401WithoutBasicHeader() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/me/notes/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- GET tests ---

    [Fact]
    public async Task GetReturns404WhenNoNoteExists() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory();
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/me/notes/42");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("not_found", body.GetProperty("error").GetString());
    }

    // --- PUT tests ---

    [Fact]
    public async Task PutCreatesNoteAndReturns204() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var putResponse = await client.PutAsync("/api/me/notes/42", JsonBody(new { noteText = "Starter town!" }));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/me/notes/42");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var body = await ReadJsonAsync(getResponse);
        Assert.Equal("Starter town!", body.GetProperty("noteText").GetString());
    }

    [Fact]
    public async Task PutUpdatesExistingNote() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        await client.PutAsync("/api/me/notes/42", JsonBody(new { noteText = "First impression" }));
        var putResponse = await client.PutAsync("/api/me/notes/42", JsonBody(new { noteText = "Updated thoughts" }));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/me/notes/42");
        var body = await ReadJsonAsync(getResponse);
        Assert.Equal("Updated thoughts", body.GetProperty("noteText").GetString());
    }

    [Fact]
    public async Task PutReturns400ForEmptyNote() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.PutAsync("/api/me/notes/42", JsonBody(new { noteText = "" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("empty_note", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PutReturns400ForWhitespaceNote() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.PutAsync("/api/me/notes/42", JsonBody(new { noteText = "   " }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("empty_note", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PutReturns400ForNoteTooLong() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var longNote = new string('a', 10_001);
        var response = await client.PutAsync("/api/me/notes/42", JsonBody(new { noteText = longNote }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("note_too_long", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PutReturns404ForUnknownLocation() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = Substitute.For<IPokemonLocationsApiClient>();
        apiClient.ExistsAsync("/locations/999").Returns(false);
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.PutAsync("/api/me/notes/999", JsonBody(new { noteText = "Hello" }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("not_found", body.GetProperty("error").GetString());
    }

    // --- DELETE tests ---

    [Fact]
    public async Task DeleteReturns204() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");
        await client.PutAsync("/api/me/notes/42", JsonBody(new { noteText = "To be deleted" }));

        var deleteResponse = await client.DeleteAsync("/api/me/notes/42");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/me/notes/42");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteIsIdempotent() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsEverything());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var first = await client.DeleteAsync("/api/me/notes/42");
        var second = await client.DeleteAsync("/api/me/notes/42");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }
}
