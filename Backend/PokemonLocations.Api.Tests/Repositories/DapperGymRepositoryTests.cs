using Dapper;
using Npgsql;
using PokemonLocations.Api.Repositories;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Repositories;

[Collection("Postgres")]
public class DapperGymRepositoryTests : IAsyncLifetime {
    private readonly PostgresFixture postgres;
    private NpgsqlDataSource dataSource = null!;

    public DapperGymRepositoryTests(PostgresFixture postgres) {
        this.postgres = postgres;
    }

    public async Task InitializeAsync() {
        dataSource = new NpgsqlDataSourceBuilder(postgres.ConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("TRUNCATE TABLE locations RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() {
        dataSource?.Dispose();
        return Task.CompletedTask;
    }

    private DapperGymRepository CreateNewRepository() => new(dataSource);

    private async Task SeedGymAsync(
        string locationName,
        string buildingName,
        string gymType,
        string badgeName,
        string gymLeader,
        int gymOrder) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var locationId = await connection.ExecuteScalarAsync<int>(
            "INSERT INTO locations (name) VALUES (@Name) RETURNING location_id",
            new { Name = locationName });
        var buildingId = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO buildings (location_id, name, building_type, description)
              VALUES (@LocationId, @Name, 'gym'::building_type, 'A gym.')
              RETURNING building_id",
            new { LocationId = locationId, Name = buildingName });
        await connection.ExecuteAsync(
            @"INSERT INTO gyms (building_id, gym_type, badge_name, gym_leader, gym_order)
              VALUES (@BuildingId, @GymType, @BadgeName, @GymLeader, @GymOrder)",
            new {
                BuildingId = buildingId,
                GymType = gymType,
                BadgeName = badgeName,
                GymLeader = gymLeader,
                GymOrder = gymOrder
            });
    }

    [Fact]
    public async Task GetAllAsyncReturnsEmptyWhenNoGymsExist() {
        var repository = CreateNewRepository();

        var gyms = await repository.GetAllAsync();

        Assert.Empty(gyms);
    }

    [Fact]
    public async Task GetAllAsyncReturnsGymsOrderedByGymOrder() {
        await SeedGymAsync("Vermilion City", "Vermilion Gym", "Electric", "Thunder Badge", "Lt. Surge", 3);
        await SeedGymAsync("Pewter City", "Pewter Gym", "Rock", "Boulder Badge", "Brock", 1);
        await SeedGymAsync("Cerulean City", "Cerulean Gym", "Water", "Cascade Badge", "Misty", 2);
        var repository = CreateNewRepository();

        var gyms = (await repository.GetAllAsync()).ToList();

        Assert.Equal(3, gyms.Count);
        Assert.Equal(new[] { 1, 2, 3 }, gyms.Select(g => g.GymOrder));
        Assert.Equal(new[] { "Brock", "Misty", "Lt. Surge" }, gyms.Select(g => g.GymLeader));
    }

    [Fact]
    public async Task GetAllAsyncIncludesLocationNameAndBuildingNameInSummary() {
        await SeedGymAsync("Pewter City", "Pewter City Gym", "Rock", "Boulder Badge", "Brock", 1);
        var repository = CreateNewRepository();

        var gyms = (await repository.GetAllAsync()).ToList();

        var gym = Assert.Single(gyms);
        Assert.Equal("Pewter City", gym.LocationName);
        Assert.Equal("Pewter City Gym", gym.BuildingName);
        Assert.Equal("Rock", gym.GymType);
        Assert.Equal("Boulder Badge", gym.BadgeName);
        Assert.Equal("Brock", gym.GymLeader);
        Assert.Equal(1, gym.GymOrder);
        Assert.True(gym.GymId > 0);
        Assert.True(gym.BuildingId > 0);
        Assert.True(gym.LocationId > 0);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsGymSummaryWhenGymExists() {
        await SeedGymAsync("Cerulean City", "Cerulean City Gym", "Water", "Cascade Badge", "Misty", 2);
        var repository = CreateNewRepository();
        var seeded = (await repository.GetAllAsync()).Single();

        var loaded = await repository.GetByIdAsync(seeded.GymId);

        Assert.NotNull(loaded);
        Assert.Equal(seeded.GymId, loaded!.GymId);
        Assert.Equal("Cerulean City", loaded.LocationName);
        Assert.Equal("Cerulean City Gym", loaded.BuildingName);
        Assert.Equal("Misty", loaded.GymLeader);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullWhenGymDoesNotExist() {
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(999_999);

        Assert.Null(loaded);
    }
}
