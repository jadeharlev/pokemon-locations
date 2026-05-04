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
}
