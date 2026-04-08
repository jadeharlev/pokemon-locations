using Dapper;
using Npgsql;

namespace PokemonLocations.Api.Repositories;

public class DatabaseHealthRepository : IDatabaseHealthRepository {
    private readonly NpgsqlDataSource dataSource;

    public DatabaseHealthRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<bool> GetHealth() {
        await using var connection = await dataSource.OpenConnectionAsync();
        var result = await connection.QuerySingleAsync<int>("SELECT 1");
        return result > 0;
    }
}
