using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class DapperBuildingRepository : IBuildingRepository {
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
}
