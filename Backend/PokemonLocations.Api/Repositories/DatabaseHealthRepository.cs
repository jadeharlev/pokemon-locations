using System.Data;
using Dapper;
using Npgsql;

namespace PokemonLocations.Api.Repositories;

public class DatabaseHealthRepository : IDatabaseHealthRepository {
    private readonly string connectionString;
    
    public DatabaseHealthRepository(IConfiguration configuration) {
        this.connectionString = configuration.GetConnectionString("Postgres")!;
    }

    private IDbConnection CreateConnection() {
        return new NpgsqlConnection(connectionString);
    }

    public async Task<bool> GetHealth() {
        using var connection = CreateConnection();
        var result = await connection.QuerySingleAsync<int>("SELECT 1");
        return result > 0;
    }
}