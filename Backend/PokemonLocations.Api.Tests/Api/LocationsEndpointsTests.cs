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
        client = factory.CreateClient();
    }

    public Task DisposeAsync() {
        client?.Dispose();
        factory?.Dispose();
        return Task.CompletedTask;
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
    public async Task PostThenGetReturnsCreatedLocation() {
        var post = await client.PostAsJsonAsync("/locations", new Location {
            Name = "Pewter City",
            Description = "Rock-types"
        });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var get = await client.GetAsync("/locations");
        var locations = await get.Content.ReadFromJsonAsync<List<Location>>();
        Assert.NotNull(locations);
        Assert.Single(locations!);
        Assert.Equal("Pewter City", locations![0].Name);
    }

    [Fact]
    public async Task PostWithoutNameReturnsBadRequest() {
        var post = await client.PostAsJsonAsync("/locations", new Location {
            Name = string.Empty,
            Description = "Missing name"
        });
        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
    }

    [Fact]
    public async Task GetByIdReturnsLocationAfterCreate() {
        var post = await client.PostAsJsonAsync("/locations", new Location { Name = "Cerulean City" });
        var created = await post.Content.ReadFromJsonAsync<Location>();
        Assert.NotNull(created);

        var get = await client.GetAsync($"/locations/{created!.LocationId}");
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

    [Fact]
    public async Task PutUpdatesExistingLocation() {
        var post = await client.PostAsJsonAsync("/locations", new Location { Name = "Old" });
        var created = await post.Content.ReadFromJsonAsync<Location>();
        Assert.NotNull(created);

        var put = await client.PutAsJsonAsync($"/locations/{created!.LocationId}", new Location {
            LocationId = created.LocationId,
            Name = "New",
            Description = "Updated"
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await client.GetAsync($"/locations/{created.LocationId}");
        var loaded = await get.Content.ReadFromJsonAsync<Location>();
        Assert.Equal("New", loaded!.Name);
        Assert.Equal("Updated", loaded.Description);
    }

    [Fact]
    public async Task DeleteRemovesLocation() {
        var post = await client.PostAsJsonAsync("/locations", new Location { Name = "Doomed" });
        var created = await post.Content.ReadFromJsonAsync<Location>();
        Assert.NotNull(created);

        var del = await client.DeleteAsync($"/locations/{created!.LocationId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var get = await client.GetAsync($"/locations/{created.LocationId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}
