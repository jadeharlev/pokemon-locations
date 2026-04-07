using Dapper;
using Npgsql;
using PokemonLocations.Api.Database;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Database;

[Collection("Postgres")]
public class MigrationRunnerTests {
    private readonly PostgresFixture postgres;

    public MigrationRunnerTests(PostgresFixture postgres) {
        this.postgres = postgres;
    }

    [Fact]
    public async Task RunCreatesAllExpectedTablesAndEnum() {
        // Migrations have already run as part of fixture inititalization.
        await using var conn = new NpgsqlConnection(postgres.ConnectionString);
        await conn.OpenAsync();

        var tables = (await conn.QueryAsync<string>(
            @"SELECT table_name FROM information_schema.tables
              WHERE table_schema = 'public'")).ToHashSet();

        Assert.Contains("locations", tables);
        Assert.Contains("location_images", tables);
        Assert.Contains("buildings", tables);
        Assert.Contains("gyms", tables);

        var enumExists = await conn.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(SELECT 1 FROM pg_type WHERE typname = 'building_type')");
        Assert.True(enumExists);
    }

    [Fact]
    public void RunningMigrationsTwiceIsANoOp() {
        var first = MigrationRunner.Run(postgres.ConnectionString);
        Assert.True(first.Successful);
        Assert.Empty(first.Scripts);
    }
}
