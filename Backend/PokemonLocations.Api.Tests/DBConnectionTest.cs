using Dapper;
using Npgsql;

namespace PokemonLocations.Api.Tests;

public class DBConnectionTest {
    [Fact]
    public async void DBConnectionWorks() {
        using var connection =
            new NpgsqlConnection(
                "Host=localhost;Port=5432;Database=pokemonlocations;Username=postgres;Password=example");
        await connection.OpenAsync();
        var result = await connection.QuerySingleAsync<int>("SELECT 1");
        Assert.Equal(1, result);
    }
}