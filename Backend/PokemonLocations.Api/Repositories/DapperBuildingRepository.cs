using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.NameTranslation;
using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class DapperBuildingRepository : IBuildingRepository {
    private static readonly NpgsqlSnakeCaseNameTranslator EnumNameTranslator = new();

    private readonly NpgsqlDataSource dataSource;
    private readonly ILogger<DapperBuildingRepository> logger;

    public DapperBuildingRepository(
        NpgsqlDataSource dataSource,
        ILogger<DapperBuildingRepository> logger) {
        this.dataSource = dataSource;
        this.logger = logger;
    }

    public async Task<IEnumerable<Building>> GetAllByLocationAsync(int locationId) {
        logger.LogInformation("Getting all buildings for location {LocationId}.", locationId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var buildings = await connection.QueryAsync<Building>(
            @"SELECT building_id, location_id, name, building_type, description, landmark_description
                FROM buildings
               WHERE location_id = @LocationId
               ORDER BY building_id",
            new { LocationId = locationId });

        logger.LogInformation("Retrieved buildings for location {LocationId}.", locationId);

        return buildings;
    }

    public async Task<Building?> GetByIdAsync(int buildingId) {
        logger.LogInformation("Getting building with ID {BuildingId}.", buildingId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var building = await connection.QuerySingleOrDefaultAsync<Building>(
            @"SELECT building_id, location_id, name, building_type, description, landmark_description
                FROM buildings
               WHERE building_id = @BuildingId",
            new { BuildingId = buildingId });

        if (building == null) {
            logger.LogWarning("Building with ID {BuildingId} was not found.", buildingId);
            return null;
        }

        if (building.BuildingType == BuildingType.Gym) {
            logger.LogInformation("Building {BuildingId} is a gym. Getting gym details.", buildingId);

            building.Gym = await connection.QuerySingleOrDefaultAsync<Gym>(
                @"SELECT gym_id, building_id, gym_type, badge_name, gym_leader, gym_order
                    FROM gyms
                   WHERE building_id = @BuildingId",
                new { BuildingId = buildingId });
        }

        logger.LogInformation("Successfully retrieved building with ID {BuildingId}.", buildingId);

        return building;
    }

    public async Task<int> CreateAsync(Building building) {
        logger.LogInformation("Creating building for location {LocationId}.", building.LocationId);

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
                logger.LogInformation("Creating gym details for building {BuildingId}.", newBuildingId);

                building.Gym.BuildingId = newBuildingId;

                await connection.ExecuteAsync(
                    @"INSERT INTO gyms (building_id, gym_type, badge_name, gym_leader, gym_order)
                      VALUES (@BuildingId, @GymType, @BadgeName, @GymLeader, @GymOrder)",
                    building.Gym,
                    transaction);
            }

            await transaction.CommitAsync();

            building.BuildingId = newBuildingId;

            logger.LogInformation("Successfully created building with ID {BuildingId}.", newBuildingId);

            return newBuildingId;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error creating building for location {LocationId}. Rolling back transaction.", building.LocationId);

            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Building building) {
        logger.LogInformation("Updating building with ID {BuildingId}.", building.BuildingId);

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
                logger.LogWarning("Cannot update building because building {BuildingId} was not found.", building.BuildingId);

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
                    logger.LogInformation("Updating gym details for building {BuildingId}.", building.BuildingId);

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
                    logger.LogInformation("Creating gym details for building {BuildingId}.", building.BuildingId);

                    await connection.ExecuteAsync(
                        @"INSERT INTO gyms (building_id, gym_type, badge_name, gym_leader, gym_order)
                          VALUES (@BuildingId, @GymType, @BadgeName, @GymLeader, @GymOrder)",
                        building.Gym,
                        transaction);
                }
            }
            else if (existingGymId.HasValue) {
                logger.LogInformation("Deleting gym details because building {BuildingId} is no longer a gym.", building.BuildingId);

                await connection.ExecuteAsync(
                    "DELETE FROM gyms WHERE building_id = @BuildingId",
                    new { building.BuildingId },
                    transaction);
            }

            await transaction.CommitAsync();

            logger.LogInformation("Successfully updated building with ID {BuildingId}.", building.BuildingId);

            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error updating building {BuildingId}. Rolling back transaction.", building.BuildingId);

            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int buildingId) {
        logger.LogInformation("Deleting building with ID {BuildingId}.", buildingId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var rows = await connection.ExecuteAsync(
            "DELETE FROM buildings WHERE building_id = @BuildingId",
            new { BuildingId = buildingId });

        if (rows == 0) {
            logger.LogWarning("Cannot delete building because building {BuildingId} was not found.", buildingId);
            return false;
        }

        logger.LogInformation("Successfully deleted building with ID {BuildingId}.", buildingId);

        return true;
    }

    private static string ToDbEnum(BuildingType buildingType) =>
        EnumNameTranslator.TranslateMemberName(buildingType.ToString());
}
