using Dapper;
using Npgsql;

namespace PokemonLocations.WebServer.Database.Repositories;

public class VisitedLocationRepository : IVisitedLocationRepository {
    private readonly NpgsqlDataSource dataSource;

    public VisitedLocationRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<int>> GetForUserAsync(int userId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<int>(
            @"SELECT location_id
                FROM user_visited_locations
               WHERE user_id = @UserId
               ORDER BY location_id",
            new { UserId = userId });
        return rows.AsList();
    }

    public async Task AddAsync(int userId, int locationId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            @"INSERT INTO user_visited_locations (user_id, location_id)
              VALUES (@UserId, @LocationId)
              ON CONFLICT (user_id, location_id) DO NOTHING",
            new { UserId = userId, LocationId = locationId });
    }

    public async Task RemoveAsync(int userId, int locationId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            @"DELETE FROM user_visited_locations
               WHERE user_id = @UserId
                 AND location_id = @LocationId",
            new { UserId = userId, LocationId = locationId });
    }
}
