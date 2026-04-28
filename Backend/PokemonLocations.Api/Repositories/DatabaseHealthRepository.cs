using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace PokemonLocations.Api.Repositories;

public class DatabaseHealthRepository : IDatabaseHealthRepository {
    private readonly NpgsqlDataSource dataSource;
    private readonly ILogger<DatabaseHealthRepository> logger;

    public DatabaseHealthRepository(
        NpgsqlDataSource dataSource,
        ILogger<DatabaseHealthRepository> logger) {
        this.dataSource = dataSource;
        this.logger = logger;
    }

    public async Task<bool> GetHealth() {
        logger.LogInformation("Checking database connection.");

        try {
            await using var connection = await dataSource.OpenConnectionAsync();

            var result = await connection.QuerySingleAsync<int>("SELECT 1");

            if (result > 0) {
                logger.LogInformation("Database connection check passed.");
                return true;
            }

            logger.LogWarning("Database connection check returned an unexpected result: {Result}.", result);
            return false;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Database connection check failed.");
            return false;
        }
    }
}
