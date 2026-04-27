using DbUp;
using DbUp.Engine;

namespace PokemonLocations.WebServer.Database;

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
