using System.Net;
using System.Net.Http.Json;
using Dapper;
using Npgsql;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Api;

[Collection("Postgres")]
public class GymsEndpointsTests : IAsyncLifetime {
    private static readonly Npgsql.NameTranslation.NpgsqlSnakeCaseNameTranslator EnumNameTranslator = new();

    private readonly PostgresFixture postgres;
    private PokemonLocationsApiFactory factory = null!;
    private HttpClient client = null!;

    public GymsEndpointsTests(PostgresFixture postgres) {
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

    private async Task<int> SeedGymBuildingAsync(
        string locationName,
        string buildingName,
        string gymType,
        string badgeName,
        string gymLeader,
        int gymOrder) {
        await using var conn = new NpgsqlConnection(postgres.ConnectionString);
        await conn.OpenAsync();

        var locationId = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO locations (name) VALUES (@Name) RETURNING location_id",
            new { Name = locationName });

        var buildingId = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO buildings (location_id, name, building_type, description)
              VALUES (@LocationId, @Name, CAST(@BuildingType AS building_type), @Description)
              RETURNING building_id",
            new {
                LocationId = locationId,
                Name = buildingName,
                BuildingType = EnumNameTranslator.TranslateMemberName(BuildingType.Gym.ToString()),
                Description = "A gym."
            });

        await conn.ExecuteAsync(
            @"INSERT INTO gyms (building_id, gym_type, badge_name, gym_leader, gym_order)
              VALUES (@BuildingId, @GymType, @BadgeName, @GymLeader, @GymOrder)",
            new { BuildingId = buildingId, GymType = gymType, BadgeName = badgeName, GymLeader = gymLeader, GymOrder = gymOrder });

        return buildingId;
    }

    [Fact]
    public async Task GetAllGymsReturnsEmptyArrayWhenNoGymsExist() {
        var response = await client.GetAsync("/gyms");

        response.EnsureSuccessStatusCode();
        var gyms = await response.Content.ReadFromJsonAsync<List<GymSummary>>();
        Assert.NotNull(gyms);
        Assert.Empty(gyms!);
    }

    [Fact]
    public async Task GetAllGymsReturnsAllGymsOrderedByGymOrder() {
        await SeedGymBuildingAsync("Vermilion City", "Vermilion Gym", "Electric", "Thunder Badge", "Lt. Surge", 3);
        await SeedGymBuildingAsync("Pewter City", "Pewter Gym", "Rock", "Boulder Badge", "Brock", 1);
        await SeedGymBuildingAsync("Cerulean City", "Cerulean Gym", "Water", "Cascade Badge", "Misty", 2);

        var response = await client.GetAsync("/gyms");

        response.EnsureSuccessStatusCode();
        var gyms = await response.Content.ReadFromJsonAsync<List<GymSummary>>();
        Assert.NotNull(gyms);
        Assert.Equal(3, gyms!.Count);
        Assert.Equal(new[] { 1, 2, 3 }, gyms.Select(g => g.GymOrder));
    }

    [Fact]
    public async Task GetByIdReturnsGymWhenItExists() {
        await SeedGymBuildingAsync("Pewter City", "Pewter City Gym", "Rock", "Boulder Badge", "Brock", 1);
        var list = await client.GetFromJsonAsync<List<GymSummary>>("/gyms");
        var seeded = list!.Single();

        var response = await client.GetAsync($"/gyms/{seeded.GymId}");

        response.EnsureSuccessStatusCode();
        var gym = await response.Content.ReadFromJsonAsync<GymSummary>();
        Assert.NotNull(gym);
        Assert.Equal("Brock", gym!.GymLeader);
        Assert.Equal("Pewter City", gym.LocationName);
    }

    [Fact]
    public async Task GetByIdReturnsNotFoundForMissingGymId() {
        var response = await client.GetAsync("/gyms/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
