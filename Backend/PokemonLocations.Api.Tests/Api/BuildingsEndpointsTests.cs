using System.Net;
using System.Net.Http.Json;
using Dapper;
using Npgsql;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Api;

[Collection("Postgres")]
public class BuildingsEndpointsTests : IAsyncLifetime {
    private static readonly Npgsql.NameTranslation.NpgsqlSnakeCaseNameTranslator EnumNameTranslator = new();

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
        client = factory.CreateAuthenticatedClient();
    }

    public Task DisposeAsync() {
        client?.Dispose();
        factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<int> SeedLocationAsync(string name = "Test Town") {
        await using var conn = new NpgsqlConnection(postgres.ConnectionString);
        await conn.OpenAsync();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO locations (name) VALUES (@Name) RETURNING location_id",
            new { Name = name });
    }

    private async Task<int> SeedBuildingAsync(
        int locationId,
        string name,
        BuildingType buildingType = BuildingType.Landmark,
        string? description = null,
        string? landmarkDescription = null) {
        await using var conn = new NpgsqlConnection(postgres.ConnectionString);
        await conn.OpenAsync();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
              VALUES (@LocationId, @Name, CAST(@BuildingType AS building_type), @Description, @LandmarkDescription)
              RETURNING building_id",
            new {
                LocationId = locationId,
                Name = name,
                BuildingType = EnumNameTranslator.TranslateMemberName(buildingType.ToString()),
                Description = description,
                LandmarkDescription = landmarkDescription
            });
    }

    private async Task SeedGymAsync(int buildingId, string gymType, string badgeName, string gymLeader, int gymOrder) {
        await using var conn = new NpgsqlConnection(postgres.ConnectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            @"INSERT INTO gyms (building_id, gym_type, badge_name, gym_leader, gym_order)
              VALUES (@BuildingId, @GymType, @BadgeName, @GymLeader, @GymOrder)",
            new { BuildingId = buildingId, GymType = gymType, BadgeName = badgeName, GymLeader = gymLeader, GymOrder = gymOrder });
    }

    [Fact]
    public async Task GetAllBuildingsReturnsNotFoundWhenLocationDoesNotExist() {
        var response = await client.GetAsync("/locations/999999/buildings");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBuildingsReturnsEmptyArrayWhenNoBuildingsExist() {
        var locationId = await SeedLocationAsync();

        var response = await client.GetAsync($"/locations/{locationId}/buildings");

        response.EnsureSuccessStatusCode();
        var buildings = await response.Content.ReadFromJsonAsync<List<Building>>();
        Assert.NotNull(buildings);
        Assert.Empty(buildings!);
    }

    [Fact]
    public async Task GetByIdReturnsSeededBuilding() {
        var locationId = await SeedLocationAsync();
        var buildingId = await SeedBuildingAsync(locationId, "Oak's Lab", BuildingType.Lab, "Research lab.");

        var get = await client.GetAsync($"/locations/{locationId}/buildings/{buildingId}");

        get.EnsureSuccessStatusCode();
        var loaded = await get.Content.ReadFromJsonAsync<Building>();
        Assert.Equal("Oak's Lab", loaded!.Name);
        Assert.Equal(BuildingType.Lab, loaded.BuildingType);
    }

    [Fact]
    public async Task GetByIdReturnsBuildingWithGymDetails() {
        var locationId = await SeedLocationAsync("Pewter City");
        var buildingId = await SeedBuildingAsync(locationId, "Pewter City Gym", BuildingType.Gym, "The gym.");
        await SeedGymAsync(buildingId, "Rock", "Boulder Badge", "Brock", 1);

        var get = await client.GetAsync($"/locations/{locationId}/buildings/{buildingId}");

        get.EnsureSuccessStatusCode();
        var loaded = await get.Content.ReadFromJsonAsync<Building>();
        Assert.NotNull(loaded);
        Assert.Equal(BuildingType.Gym, loaded!.BuildingType);
        Assert.NotNull(loaded.Gym);
        Assert.Equal("Brock", loaded.Gym!.GymLeader);
    }

    [Fact]
    public async Task GetByIdReturnsNotFoundForMissingBuildingId() {
        var locationId = await SeedLocationAsync();

        var response = await client.GetAsync($"/locations/{locationId}/buildings/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/locations/1/buildings")]
    [InlineData("PUT", "/locations/1/buildings/1")]
    [InlineData("DELETE", "/locations/1/buildings/1")]
    public async Task WriteVerbsReturnMethodNotAllowed(string method, string path) {
        var request = new HttpRequestMessage(new HttpMethod(method), path) {
            Content = JsonContent.Create(new Building { Name = "x", BuildingType = BuildingType.Landmark })
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
