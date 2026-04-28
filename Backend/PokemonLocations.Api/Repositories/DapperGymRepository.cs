using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class DapperGymRepository : IGymRepository {
    private readonly NpgsqlDataSource dataSource;
    private readonly ILogger<DapperGymRepository> logger;

    private const string SelectGymSummary = @"
        SELECT g.gym_id,
               g.building_id,
               b.location_id,
               l.name AS location_name,
               b.name AS building_name,
               g.gym_type,
               g.badge_name,
               g.gym_leader,
               g.gym_order
          FROM gyms g
          JOIN buildings b ON b.building_id = g.building_id
          JOIN locations l ON l.location_id = b.location_id";

    public DapperGymRepository(
        NpgsqlDataSource dataSource,
        ILogger<DapperGymRepository> logger) {
        this.dataSource = dataSource;
        this.logger = logger;
    }

    public async Task<IEnumerable<GymSummary>> GetAllAsync() {
        logger.LogInformation("Getting all gyms from the database.");

        await using var connection = await dataSource.OpenConnectionAsync();

        var gyms = await connection.QueryAsync<GymSummary>(
            SelectGymSummary + " ORDER BY g.gym_order");

        logger.LogInformation("Successfully retrieved all gyms from the database.");

        return gyms;
    }

    public async Task<GymSummary?> GetByIdAsync(int gymId) {
        logger.LogInformation("Getting gym with ID {GymId} from the database.", gymId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var gym = await connection.QuerySingleOrDefaultAsync<GymSummary>(
            SelectGymSummary + " WHERE g.gym_id = @GymId",
            new { GymId = gymId });

        if (gym == null) {
            logger.LogWarning("Gym with ID {GymId} was not found in the database.", gymId);
            return null;
        }

        logger.LogInformation("Successfully retrieved gym with ID {GymId} from the database.", gymId);

        return gym;
    }
}
