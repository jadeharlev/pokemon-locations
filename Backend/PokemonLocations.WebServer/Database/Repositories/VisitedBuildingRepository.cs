using Dapper;
using Npgsql;

namespace PokemonLocations.WebServer.Database.Repositories;

public class VisitedBuildingRepository : IVisitedBuildingRepository {
    private readonly NpgsqlDataSource dataSource;

    public VisitedBuildingRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<int>> GetForUserAsync(int userId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<int>(
            @"SELECT building_id
                FROM user_visited_buildings
               WHERE user_id = @UserId
               ORDER BY building_id",
            new { UserId = userId });
        return rows.AsList();
    }

    public async Task AddAsync(int userId, int locationId, int buildingId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            @"INSERT INTO user_visited_buildings (user_id, location_id, building_id)
              VALUES (@UserId, @LocationId, @BuildingId)
              ON CONFLICT (user_id, building_id) DO NOTHING",
            new { UserId = userId, LocationId = locationId, BuildingId = buildingId });
    }

    public async Task RemoveAsync(int userId, int buildingId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            @"DELETE FROM user_visited_buildings
               WHERE user_id = @UserId
                 AND building_id = @BuildingId",
            new { UserId = userId, BuildingId = buildingId });
    }

    public async Task<IReadOnlyList<int>> GetDistinctLocationIdsForUserAsync(int userId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<int>(
            @"SELECT DISTINCT location_id
                FROM user_visited_buildings
               WHERE user_id = @UserId
               ORDER BY location_id",
            new { UserId = userId });
        return rows.AsList();
    }
}
