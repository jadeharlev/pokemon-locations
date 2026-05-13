using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Database;
using Testcontainers.PostgreSql;

namespace PokemonLocations.WebServer.Tests.Infrastructure;

public class PostgresFixture : IAsyncLifetime {
    private static readonly Lazy<Task<PostgreSqlContainer>> sharedContainer = new(StartSharedContainerAsync);

    private string connectionString = null!;

    public string ConnectionString => connectionString;

    public async Task InitializeAsync() {
        connectionString = await AllocateMigratedDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    internal static Task<PostgreSqlContainer> GetSharedContainerAsync() => sharedContainer.Value;

    internal static async Task<string> AllocateMigratedDatabaseAsync() {
        var container = await sharedContainer.Value;
        var dbName = "test_" + Guid.NewGuid().ToString("N");

        await using (var adminConn = new NpgsqlConnection(container.GetConnectionString())) {
            await adminConn.OpenAsync();
            await adminConn.ExecuteAsync($"CREATE DATABASE \"{dbName}\"");
        }

        var connString = new NpgsqlConnectionStringBuilder(container.GetConnectionString()) {
            Database = dbName
        }.ConnectionString;

        var result = MigrationRunner.Run(connString);
        if (!result.Successful) {
            throw new InvalidOperationException("Test DB migration failed", result.Error);
        }
        return connString;
    }

    private static async Task<PostgreSqlContainer> StartSharedContainerAsync() {
        var builder = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("example")
            .WithCommand("-c", "max_connections=400")
            .Build();
        await builder.StartAsync();
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        return builder;
    }
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture> { }
