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

    private static Building NewLandmarkBuilding(int locationId, string name = "Test Landmark") => new() {
        LocationId = locationId,
        Name = name,
        BuildingType = BuildingType.Landmark,
        Description = "A landmark.",
        LandmarkDescription = "Notable feature."
    };

    private static Building NewGymBuilding(int locationId, string name = "Test Gym") => new() {
        LocationId = locationId,
        Name = name,
        BuildingType = BuildingType.Gym,
        Description = "A gym.",
        Gym = new Gym {
            GymType = "Rock",
            BadgeName = "Boulder Badge",
            GymLeader = "Brock",
            GymOrder = 1
        }
    };

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
        var repository = CreateNewRepository();

        await repository.CreateAsync(NewLandmarkBuilding(palletId, "Oak's Lab"));
        await repository.CreateAsync(NewLandmarkBuilding(palletId, "Player's House"));
        await repository.CreateAsync(NewLandmarkBuilding(pewterId, "Pewter Museum"));

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
        var repository = CreateNewRepository();
        var newId = await repository.CreateAsync(NewLandmarkBuilding(locationId, "Pewter Museum"));

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Equal("Pewter Museum", loaded!.Name);
        Assert.Equal(BuildingType.Landmark, loaded.BuildingType);
        Assert.Null(loaded.Gym);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsBuildingWithGymWhenTypeIsGym() {
        var locationId = await SeedLocationAsync("Pewter City");
        var repository = CreateNewRepository();
        var newId = await repository.CreateAsync(NewGymBuilding(locationId, "Pewter City Gym"));

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

    [Fact]
    public async Task CreateAsyncInsertsNonGymBuildingAndReturnsId() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var newId = await repository.CreateAsync(NewLandmarkBuilding(locationId));

        Assert.True(newId > 0);
        await using var connection = await dataSource.OpenConnectionAsync();
        var buildingCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM buildings");
        var gymCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM gyms");
        Assert.Equal(1, buildingCount);
        Assert.Equal(0, gymCount);
    }

    [Fact]
    public async Task CreateAsyncInsertsGymBuildingAndGymRowAtomically() {
        var locationId = await SeedLocationAsync("Pewter City");
        var repository = CreateNewRepository();

        var newId = await repository.CreateAsync(NewGymBuilding(locationId, "Pewter City Gym"));

        await using var connection = await dataSource.OpenConnectionAsync();
        var buildingCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM buildings");
        var gymCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM gyms");
        var gymBuildingId = await connection.ExecuteScalarAsync<int>("SELECT building_id FROM gyms LIMIT 1");
        Assert.Equal(1, buildingCount);
        Assert.Equal(1, gymCount);
        Assert.Equal(newId, gymBuildingId);
    }

    [Fact]
    public async Task CreateAsyncRollsBackBothRowsWhenGymInsertFails() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var building = NewGymBuilding(locationId, "Doomed Gym");
        building.Gym!.GymType = null!;

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await repository.CreateAsync(building));

        await using var connection = await dataSource.OpenConnectionAsync();
        var buildingCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM buildings");
        var gymCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM gyms");
        Assert.Equal(0, buildingCount);
        Assert.Equal(0, gymCount);
    }

    [Fact]
    public async Task UpdateAsyncMutatesExistingBuildingAndReturnsTrue() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();
        var newId = await repository.CreateAsync(NewLandmarkBuilding(locationId, "Old Name"));

        var update = new Building {
            BuildingId = newId,
            LocationId = locationId,
            Name = "New Name",
            BuildingType = BuildingType.Landmark,
            Description = "Updated description",
            LandmarkDescription = "Updated landmark"
        };

        var result = await repository.UpdateAsync(update);

        Assert.True(result);
        var loaded = await repository.GetByIdAsync(newId);
        Assert.Equal("New Name", loaded!.Name);
        Assert.Equal("Updated description", loaded.Description);
        Assert.Equal("Updated landmark", loaded.LandmarkDescription);
    }

    [Fact]
    public async Task UpdateAsyncChangingTypeFromGymToLandmarkDeletesGymRow() {
        var locationId = await SeedLocationAsync("Pewter City");
        var repository = CreateNewRepository();
        var newId = await repository.CreateAsync(NewGymBuilding(locationId, "Pewter City Gym"));

        var update = new Building {
            BuildingId = newId,
            LocationId = locationId,
            Name = "Old Gym Memorial",
            BuildingType = BuildingType.Landmark,
            Description = "Used to be a gym.",
            LandmarkDescription = "A memorial to Brock.",
            Gym = null
        };

        var result = await repository.UpdateAsync(update);

        Assert.True(result);
        var loaded = await repository.GetByIdAsync(newId);
        Assert.Equal(BuildingType.Landmark, loaded!.BuildingType);
        Assert.Null(loaded.Gym);

        await using var connection = await dataSource.OpenConnectionAsync();
        var gymCount = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM gyms WHERE building_id = @BuildingId",
            new { BuildingId = newId });
        Assert.Equal(0, gymCount);
    }

    [Fact]
    public async Task UpdateAsyncChangingTypeFromLandmarkToGymInsertsGymRow() {
        var locationId = await SeedLocationAsync("Cerulean City");
        var repository = CreateNewRepository();
        var newId = await repository.CreateAsync(NewLandmarkBuilding(locationId, "Future Gym Site"));

        var update = new Building {
            BuildingId = newId,
            LocationId = locationId,
            Name = "Cerulean City Gym",
            BuildingType = BuildingType.Gym,
            Description = "The Cerulean City Gym.",
            Gym = new Gym {
                GymType = "Water",
                BadgeName = "Cascade Badge",
                GymLeader = "Misty",
                GymOrder = 2
            }
        };

        var result = await repository.UpdateAsync(update);

        Assert.True(result);
        var loaded = await repository.GetByIdAsync(newId);
        Assert.Equal(BuildingType.Gym, loaded!.BuildingType);
        Assert.NotNull(loaded.Gym);
        Assert.Equal("Misty", loaded.Gym!.GymLeader);
    }

    [Fact]
    public async Task UpdateAsyncReturnsFalseWhenBuildingDoesNotExist() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var result = await repository.UpdateAsync(new Building {
            BuildingId = 999_999,
            LocationId = locationId,
            Name = "Ghost",
            BuildingType = BuildingType.Lab
        });

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsyncRemovesBuildingAndCascadesGymRow() {
        var locationId = await SeedLocationAsync("Pewter City");
        var repository = CreateNewRepository();
        var newId = await repository.CreateAsync(NewGymBuilding(locationId, "Pewter City Gym"));

        var result = await repository.DeleteAsync(newId);

        Assert.True(result);
        Assert.Null(await repository.GetByIdAsync(newId));

        await using var connection = await dataSource.OpenConnectionAsync();
        var gymCount = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM gyms WHERE building_id = @BuildingId",
            new { BuildingId = newId });
        Assert.Equal(0, gymCount);
    }

    [Fact]
    public async Task DeleteAsyncReturnsFalseWhenBuildingDoesNotExist() {
        var repository = CreateNewRepository();

        var result = await repository.DeleteAsync(999_999);

        Assert.False(result);
    }
}
