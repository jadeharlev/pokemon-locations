using Dapper;
using Npgsql;
using Npgsql.NameTranslation;
using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class DapperBuildingRepository : IBuildingRepository {
    private static readonly NpgsqlSnakeCaseNameTranslator EnumNameTranslator = new();

    private readonly NpgsqlDataSource dataSource;

    public DapperBuildingRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<IEnumerable<Building>> GetAllByLocationAsync(int locationId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QueryAsync<Building>(
            @"SELECT building_id, location_id, name, building_type, description, landmark_description
                FROM buildings
               WHERE location_id = @LocationId
               ORDER BY building_id",
            new { LocationId = locationId });
    }

    public async Task<Building?> GetByIdAsync(int buildingId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var building = await connection.QuerySingleOrDefaultAsync<Building>(
            @"SELECT building_id, location_id, name, building_type, description, landmark_description
                FROM buildings
               WHERE building_id = @BuildingId",
            new { BuildingId = buildingId });

        if (building == null) return null;

        if (building.BuildingType == BuildingType.Gym) {
            building.Gym = await connection.QuerySingleOrDefaultAsync<Gym>(
                @"SELECT gym_id, building_id, gym_type, badge_name, gym_leader, gym_order
                    FROM gyms
                   WHERE building_id = @BuildingId",
                new { BuildingId = buildingId });
        }

        return building;
    }

    public async Task<int> CreateAsync(Building building) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try {
            var newBuildingId = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
                  VALUES (@LocationId, @Name, CAST(@BuildingType AS building_type), @Description, @LandmarkDescription)
                  RETURNING building_id",
                new {
                    building.LocationId,
                    building.Name,
                    BuildingType = ToDbEnum(building.BuildingType),
                    building.Description,
                    building.LandmarkDescription
                },
                transaction);

            if (building.BuildingType == BuildingType.Gym && building.Gym != null) {
                building.Gym.BuildingId = newBuildingId;
                await connection.ExecuteAsync(
                    @"INSERT INTO gyms (building_id, gym_type, badge_name, gym_leader, gym_order)
                      VALUES (@BuildingId, @GymType, @BadgeName, @GymLeader, @GymOrder)",
                    building.Gym,
                    transaction);
            }

            await transaction.CommitAsync();
            building.BuildingId = newBuildingId;
            return newBuildingId;
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Building building) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try {
            var rows = await connection.ExecuteAsync(
                @"UPDATE buildings
                     SET location_id = @LocationId,
                         name = @Name,
                         building_type = CAST(@BuildingType AS building_type),
                         description = @Description,
                         landmark_description = @LandmarkDescription
                   WHERE building_id = @BuildingId",
                new {
                    building.BuildingId,
                    building.LocationId,
                    building.Name,
                    BuildingType = ToDbEnum(building.BuildingType),
                    building.Description,
                    building.LandmarkDescription
                },
                transaction);

            if (rows == 0) {
                await transaction.RollbackAsync();
                return false;
            }

            var existingGymId = await connection.ExecuteScalarAsync<int?>(
                "SELECT gym_id FROM gyms WHERE building_id = @BuildingId",
                new { building.BuildingId },
                transaction);

            if (building.BuildingType == BuildingType.Gym && building.Gym != null) {
                building.Gym.BuildingId = building.BuildingId;
                if (existingGymId.HasValue) {
                    await connection.ExecuteAsync(
                        @"UPDATE gyms
                             SET gym_type = @GymType,
                                 badge_name = @BadgeName,
                                 gym_leader = @GymLeader,
                                 gym_order = @GymOrder
                           WHERE building_id = @BuildingId",
                        building.Gym,
                        transaction);
                }
                else {
                    await connection.ExecuteAsync(
                        @"INSERT INTO gyms (building_id, gym_type, badge_name, gym_leader, gym_order)
                          VALUES (@BuildingId, @GymType, @BadgeName, @GymLeader, @GymOrder)",
                        building.Gym,
                        transaction);
                }
            }
            else if (existingGymId.HasValue) {
                await connection.ExecuteAsync(
                    "DELETE FROM gyms WHERE building_id = @BuildingId",
                    new { building.BuildingId },
                    transaction);
            }

            await transaction.CommitAsync();
            return true;
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int buildingId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.ExecuteAsync(
            "DELETE FROM buildings WHERE building_id = @BuildingId",
            new { BuildingId = buildingId });
        return rows > 0;
    }

    private static string ToDbEnum(BuildingType buildingType) =>
        EnumNameTranslator.TranslateMemberName(buildingType.ToString());
}
