using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using PokemonLocations.Api.Database;
using Testcontainers.PostgreSql;

namespace PokemonLocations.Api.Tests.Infrastructure;

public class PostgresFixture : IAsyncLifetime {
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("pokemonlocations_test")
        .WithUsername("postgres")
        .WithPassword("example")
        .Build();

    public string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync() {
        await container.StartAsync();
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        var result = MigrationRunner.Run(
            ConnectionString,
            NullLogger.Instance);

        if (!result.Successful) {
            throw new InvalidOperationException("Test DB migration failed", result.Error);
        }
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture> { }
