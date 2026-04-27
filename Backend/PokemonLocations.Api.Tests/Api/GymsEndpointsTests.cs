using System.Net;
using System.Net.Http.Json;
using Dapper;
using Npgsql;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Api;

[Collection("Postgres")]
public class GymsEndpointsTests : IAsyncLifetime {
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

    private async Task<int> CreateGymBuildingAsync(
        string locationName,
        string buildingName,
        string gymType,
        string badgeName,
        string gymLeader,
        int gymOrder) {
        var locResponse = await client.PostAsJsonAsync("/locations", new Location { Name = locationName });
        var location = await locResponse.Content.ReadFromJsonAsync<Location>();

        var buildingResponse = await client.PostAsJsonAsync(
            $"/locations/{location!.LocationId}/buildings",
            new Building {
                Name = buildingName,
                BuildingType = BuildingType.Gym,
                Description = "A gym.",
                Gym = new Gym {
                    GymType = gymType,
                    BadgeName = badgeName,
                    GymLeader = gymLeader,
                    GymOrder = gymOrder
                }
            });
        buildingResponse.EnsureSuccessStatusCode();
        var building = await buildingResponse.Content.ReadFromJsonAsync<Building>();
        return building!.BuildingId;
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
        await CreateGymBuildingAsync("Vermilion City", "Vermilion Gym", "Electric", "Thunder Badge", "Lt. Surge", 3);
        await CreateGymBuildingAsync("Pewter City", "Pewter Gym", "Rock", "Boulder Badge", "Brock", 1);
        await CreateGymBuildingAsync("Cerulean City", "Cerulean Gym", "Water", "Cascade Badge", "Misty", 2);

        var response = await client.GetAsync("/gyms");

        response.EnsureSuccessStatusCode();
        var gyms = await response.Content.ReadFromJsonAsync<List<GymSummary>>();
        Assert.NotNull(gyms);
        Assert.Equal(3, gyms!.Count);
        Assert.Equal(new[] { 1, 2, 3 }, gyms.Select(g => g.GymOrder));
    }

    [Fact]
    public async Task GetByIdReturnsGymWhenItExists() {
        await CreateGymBuildingAsync("Pewter City", "Pewter City Gym", "Rock", "Boulder Badge", "Brock", 1);
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
