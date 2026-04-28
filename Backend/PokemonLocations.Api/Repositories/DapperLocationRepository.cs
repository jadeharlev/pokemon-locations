using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class DapperLocationRepository : ILocationRepository {
    private readonly NpgsqlDataSource dataSource;
    private readonly ILogger<DapperLocationRepository> logger;

    public DapperLocationRepository(
        NpgsqlDataSource dataSource,
        ILogger<DapperLocationRepository> logger) {
        this.dataSource = dataSource;
        this.logger = logger;
    }

    public async Task<IEnumerable<Location>> GetAllAsync() {
        logger.LogInformation("Getting all locations from the database.");

        await using var connection = await dataSource.OpenConnectionAsync();

        var locations = await connection.QueryAsync<Location>(
            "SELECT location_id, name, description, video_url FROM locations ORDER BY location_id");

        logger.LogInformation("Successfully retrieved all locations from the database.");

        return locations;
    }

    public async Task<Location?> GetByIdAsync(int locationId) {
        logger.LogInformation("Getting location with ID {LocationId} from the database.", locationId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var location = await connection.QuerySingleOrDefaultAsync<Location>(
            "SELECT location_id, name, description, video_url FROM locations WHERE location_id = @LocationId",
            new { LocationId = locationId });

        if (location == null) {
            logger.LogWarning("Location with ID {LocationId} was not found in the database.", locationId);
            return null;
        }

        logger.LogInformation("Successfully retrieved location with ID {LocationId} from the database.", locationId);

        return location;
    }

    public async Task<int> CreateAsync(Location location) {
        logger.LogInformation("Creating location in the database.");

        await using var connection = await dataSource.OpenConnectionAsync();

        var newLocationId = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO locations (name, description, video_url)
              VALUES (@Name, @Description, @VideoUrl)
              RETURNING location_id",
            location);

        logger.LogInformation("Successfully created location with ID {LocationId}.", newLocationId);

        return newLocationId;
    }

    public async Task<bool> UpdateAsync(Location location) {
        logger.LogInformation("Updating location with ID {LocationId} in the database.", location.LocationId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var rows = await connection.ExecuteAsync(
            @"UPDATE locations
                 SET name = @Name,
                     description = @Description,
                     video_url = @VideoUrl
               WHERE location_id = @LocationId",
            location);

        if (rows == 0) {
            logger.LogWarning("Cannot update location because location with ID {LocationId} was not found.", location.LocationId);
            return false;
        }

        logger.LogInformation("Successfully updated location with ID {LocationId}.", location.LocationId);

        return true;
    }

    public async Task<bool> DeleteAsync(int locationId) {
        logger.LogInformation("Deleting location with ID {LocationId} from the database.", locationId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var rows = await connection.ExecuteAsync(
            "DELETE FROM locations WHERE location_id = @LocationId",
            new { LocationId = locationId });

        if (rows == 0) {
            logger.LogWarning("Cannot delete location because location with ID {LocationId} was not found.", locationId);
            return false;
        }

        logger.LogInformation("Successfully deleted location with ID {LocationId}.", locationId);

        return true;
    }
}
