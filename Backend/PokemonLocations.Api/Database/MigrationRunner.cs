using DbUp;
using DbUp.Engine;

namespace PokemonLocations.Api.Database;

public static class MigrationRunner {
    public static DatabaseUpgradeResult Run(string connectionString) {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(MigrationRunner).Assembly)
            .LogToConsole()
            .Build();
        return upgrader.PerformUpgrade();
    }
}
