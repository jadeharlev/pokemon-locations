using System.Net;
using System.Net.Http.Json;
using Dapper;
using Npgsql;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Api;

[Collection("Postgres")]
public class BuildingsEndpointsTests : IAsyncLifetime {
    private readonly PostgresFixture postgres;
    private PokemonLocationsApiFactory factory = null!;
    private HttpClient client = null!;

    public BuildingsEndpointsTests(PostgresFixture postgres) {
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

    private async Task<int> CreateLocationAsync(string name = "Test Town") {
        var response = await client.PostAsJsonAsync("/locations", new Location { Name = name });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Location>();
        return created!.LocationId;
    }

    [Fact]
    public async Task GetAllBuildingsReturnsNotFoundWhenLocationDoesNotExist() {
        var response = await client.GetAsync("/locations/999999/buildings");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBuildingsReturnsEmptyArrayWhenNoBuildingsExist() {
        var locationId = await CreateLocationAsync();

        var response = await client.GetAsync($"/locations/{locationId}/buildings");

        response.EnsureSuccessStatusCode();
        var buildings = await response.Content.ReadFromJsonAsync<List<Building>>();
        Assert.NotNull(buildings);
        Assert.Empty(buildings!);
    }

    [Fact]
    public async Task PostLandmarkBuildingReturnsCreated() {
        var locationId = await CreateLocationAsync("Pewter City");

        var response = await client.PostAsJsonAsync($"/locations/{locationId}/buildings", new Building {
            Name = "Pewter Museum",
            BuildingType = BuildingType.Landmark,
            Description = "A museum.",
            LandmarkDescription = "Has fossils."
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Building>();
        Assert.NotNull(created);
        Assert.True(created!.BuildingId > 0);
        Assert.Equal(locationId, created.LocationId);
        Assert.Equal(BuildingType.Landmark, created.BuildingType);
    }

    [Fact]
    public async Task PostGymBuildingPersistsGymDetails() {
        var locationId = await CreateLocationAsync("Pewter City");

        var response = await client.PostAsJsonAsync($"/locations/{locationId}/buildings", new Building {
            Name = "Pewter City Gym",
            BuildingType = BuildingType.Gym,
            Description = "The gym.",
            Gym = new Gym {
                GymType = "Rock",
                BadgeName = "Boulder Badge",
                GymLeader = "Brock",
                GymOrder = 1
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Building>();
        Assert.NotNull(created);

        var get = await client.GetAsync($"/locations/{locationId}/buildings/{created!.BuildingId}");
        get.EnsureSuccessStatusCode();
        var loaded = await get.Content.ReadFromJsonAsync<Building>();
        Assert.NotNull(loaded);
        Assert.Equal(BuildingType.Gym, loaded!.BuildingType);
        Assert.NotNull(loaded.Gym);
        Assert.Equal("Brock", loaded.Gym!.GymLeader);
    }

    [Fact]
    public async Task PostBuildingReturnsNotFoundWhenLocationDoesNotExist() {
        var response = await client.PostAsJsonAsync("/locations/999999/buildings", new Building {
            Name = "Ghost",
            BuildingType = BuildingType.Landmark
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostBuildingWithoutNameReturnsBadRequest() {
        var locationId = await CreateLocationAsync();

        var response = await client.PostAsJsonAsync($"/locations/{locationId}/buildings", new Building {
            Name = string.Empty,
            BuildingType = BuildingType.Landmark
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetByIdReturnsBuildingAfterCreate() {
        var locationId = await CreateLocationAsync();
        var post = await client.PostAsJsonAsync($"/locations/{locationId}/buildings", new Building {
            Name = "Oak's Lab",
            BuildingType = BuildingType.Lab,
            Description = "Research lab."
        });
        var created = await post.Content.ReadFromJsonAsync<Building>();

        var get = await client.GetAsync($"/locations/{locationId}/buildings/{created!.BuildingId}");

        get.EnsureSuccessStatusCode();
        var loaded = await get.Content.ReadFromJsonAsync<Building>();
        Assert.Equal("Oak's Lab", loaded!.Name);
        Assert.Equal(BuildingType.Lab, loaded.BuildingType);
    }

    [Fact]
    public async Task GetByIdReturnsNotFoundForMissingBuildingId() {
        var locationId = await CreateLocationAsync();

        var response = await client.GetAsync($"/locations/{locationId}/buildings/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutUpdatesExistingBuilding() {
        var locationId = await CreateLocationAsync();
        var post = await client.PostAsJsonAsync($"/locations/{locationId}/buildings", new Building {
            Name = "Old Name",
            BuildingType = BuildingType.Landmark,
            LandmarkDescription = "Old landmark"
        });
        var created = await post.Content.ReadFromJsonAsync<Building>();

        var put = await client.PutAsJsonAsync(
            $"/locations/{locationId}/buildings/{created!.BuildingId}",
            new Building {
                BuildingId = created.BuildingId,
                LocationId = locationId,
                Name = "New Name",
                BuildingType = BuildingType.Landmark,
                LandmarkDescription = "New landmark"
            });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await client.GetAsync($"/locations/{locationId}/buildings/{created.BuildingId}");
        var loaded = await get.Content.ReadFromJsonAsync<Building>();
        Assert.Equal("New Name", loaded!.Name);
        Assert.Equal("New landmark", loaded.LandmarkDescription);
    }

    [Fact]
    public async Task DeleteRemovesBuilding() {
        var locationId = await CreateLocationAsync();
        var post = await client.PostAsJsonAsync($"/locations/{locationId}/buildings", new Building {
            Name = "Doomed",
            BuildingType = BuildingType.Landmark
        });
        var created = await post.Content.ReadFromJsonAsync<Building>();

        var del = await client.DeleteAsync($"/locations/{locationId}/buildings/{created!.BuildingId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var get = await client.GetAsync($"/locations/{locationId}/buildings/{created.BuildingId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}
