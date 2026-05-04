using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Repositories;

[Collection("Postgres")]
public class DapperBuildingRepositoryTests : IAsyncLifetime {
    private readonly PostgresFixture postgres;
    private NpgsqlDataSource dataSource = null!;

    public DapperBuildingRepositoryTests(PostgresFixture postgres) {
        this.postgres = postgres;
    }

    public async Task InitializeAsync() {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgres.ConnectionString);
        dataSourceBuilder.MapEnum<BuildingType>(
            pgName: "building_type",
            nameTranslator: new Npgsql.NameTranslation.NpgsqlSnakeCaseNameTranslator());
        dataSource = dataSourceBuilder.Build();
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("TRUNCATE TABLE locations RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() {
        dataSource?.Dispose();
        return Task.CompletedTask;
    }

    private DapperBuildingRepository CreateNewRepository() {
        return new DapperBuildingRepository(
            dataSource,
            NullLogger<DapperBuildingRepository>.Instance);
    }

    private async Task<int> SeedLocationAsync(string name = "Test Town") {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            "INSERT INTO locations (name) VALUES (@Name) RETURNING location_id",
            new { Name = name });
    }

    private static readonly Npgsql.NameTranslation.NpgsqlSnakeCaseNameTranslator EnumNameTranslator = new();

    private async Task<int> SeedBuildingAsync(
        int locationId,
        string name,
        BuildingType buildingType = BuildingType.Landmark,
        string? description = "A building.",
        string? landmarkDescription = "A notable feature.") {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
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

    private async Task SeedGymAsync(
        int buildingId,
        string gymType = "Rock",
        string badgeName = "Boulder Badge",
        string gymLeader = "Brock",
        int gymOrder = 1) {
        await using var connection = await dataSource.OpenConnectionAsync();
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
    public async Task GetAllByLocationAsyncReturnsEmptyWhenNoBuildingsExist() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var buildings = await repository.GetAllByLocationAsync(locationId);

        Assert.Empty(buildings);
    }

    [Fact]
    public async Task GetAllByLocationAsyncReturnsOnlyBuildingsForGivenLocation() {
        var palletId = await SeedLocationAsync("Pallet Town");
        var pewterId = await SeedLocationAsync("Pewter City");
        await SeedBuildingAsync(palletId, "Oak's Lab");
        await SeedBuildingAsync(palletId, "Player's House");
        await SeedBuildingAsync(pewterId, "Pewter Museum");
        var repository = CreateNewRepository();

        var palletBuildings = (await repository.GetAllByLocationAsync(palletId)).ToList();

        Assert.Equal(2, palletBuildings.Count);
        Assert.All(palletBuildings, b => Assert.Equal(palletId, b.LocationId));
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullWhenBuildingDoesNotExist() {
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(999_999);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsBuildingWithoutGymWhenTypeIsNotGym() {
        var locationId = await SeedLocationAsync();
        var newId = await SeedBuildingAsync(locationId, "Pewter Museum", BuildingType.Landmark);
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Equal("Pewter Museum", loaded!.Name);
        Assert.Equal(BuildingType.Landmark, loaded.BuildingType);
        Assert.Null(loaded.Gym);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsBuildingWithGymWhenTypeIsGym() {
        var locationId = await SeedLocationAsync("Pewter City");
        var newId = await SeedBuildingAsync(locationId, "Pewter City Gym", BuildingType.Gym);
        await SeedGymAsync(newId);
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Equal(BuildingType.Gym, loaded!.BuildingType);
        Assert.NotNull(loaded.Gym);
        Assert.Equal("Rock", loaded.Gym!.GymType);
        Assert.Equal("Boulder Badge", loaded.Gym.BadgeName);
        Assert.Equal("Brock", loaded.Gym.GymLeader);
        Assert.Equal(1, loaded.Gym.GymOrder);
        Assert.Equal(newId, loaded.Gym.BuildingId);
    }
}
