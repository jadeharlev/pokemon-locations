using System.Net;
using System.Net.Http.Json;
using Dapper;
using Npgsql;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Api;

[Collection("Postgres")]
public class LocationsEndpointsTests : IAsyncLifetime {
    private readonly PostgresFixture postgres;
    private PokemonLocationsApiFactory factory = null!;
    private HttpClient client = null!;

    public LocationsEndpointsTests(PostgresFixture postgres) {
        this.postgres = postgres;
    }

    public async Task InitializeAsync() {
        await using (var conn = new NpgsqlConnection(postgres.ConnectionString)) {
            await conn.OpenAsync();
            await conn.ExecuteAsync("TRUNCATE TABLE locations RESTART IDENTITY CASCADE");
        }

        factory = new PokemonLocationsApiFactory(postgres.ConnectionString);
        client = factory.CreateAuthenticatedClient();
    }

    public Task DisposeAsync() {
        client?.Dispose();
        factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<int> SeedLocationAsync(string name, string? description = null) {
        await using var conn = new NpgsqlConnection(postgres.ConnectionString);
        await conn.OpenAsync();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO locations (name, description) VALUES (@Name, @Description) RETURNING location_id",
            new { Name = name, Description = description });
    }

    [Fact]
    public async Task GetLocationsOnEmptyDbReturnsOkAndEmptyArray() {
        var response = await client.GetAsync("/locations");
        response.EnsureSuccessStatusCode();

        var locations = await response.Content.ReadFromJsonAsync<List<Location>>();
        Assert.NotNull(locations);
        Assert.Empty(locations!);
    }

    [Fact]
    public async Task GetLocationsReturnsSeededRows() {
        await SeedLocationAsync("Pewter City", "Rock-types");

        var get = await client.GetAsync("/locations");
        get.EnsureSuccessStatusCode();

        var locations = await get.Content.ReadFromJsonAsync<List<Location>>();
        Assert.NotNull(locations);
        Assert.Single(locations!);
        Assert.Equal("Pewter City", locations![0].Name);
    }

    [Fact]
    public async Task GetByIdReturnsSeededLocation() {
        var newId = await SeedLocationAsync("Cerulean City");

        var get = await client.GetAsync($"/locations/{newId}");

        get.EnsureSuccessStatusCode();
        var loaded = await get.Content.ReadFromJsonAsync<Location>();
        Assert.NotNull(loaded);
        Assert.Equal("Cerulean City", loaded!.Name);
    }

    [Fact]
    public async Task GetByIdReturnsNotFoundForMissingId() {
        var get = await client.GetAsync("/locations/999999");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/locations")]
    [InlineData("PUT", "/locations/1")]
    [InlineData("DELETE", "/locations/1")]
    public async Task WriteVerbsReturnMethodNotAllowed(string method, string path) {
        var request = new HttpRequestMessage(new HttpMethod(method), path) {
            Content = JsonContent.Create(new Location { Name = "x" })
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
