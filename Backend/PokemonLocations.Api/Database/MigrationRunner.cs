using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Logging;

namespace PokemonLocations.Api.Database;

public static class MigrationRunner {
    public static DatabaseUpgradeResult Run(string connectionString, ILogger logger) {
        logger.LogInformation("Starting database migration.");

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(MigrationRunner).Assembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (result.Successful) {
            logger.LogInformation("Database migration completed successfully.");
        }
        else {
            logger.LogError(result.Error, "Database migration failed.");
        }

        return result;
    }
}
