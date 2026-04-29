using Dapper;
using Npgsql;

namespace PokemonLocations.WebServer.Database.Repositories;

public class BadgeRepository : IBadgeRepository {
    private readonly NpgsqlDataSource dataSource;

    public BadgeRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<string>> GetForUserAsync(int userId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<string>(
            @"SELECT badge::text
                FROM user_badges
               WHERE user_id = @UserId
               ORDER BY badge",
            new { UserId = userId });
        return rows.AsList();
    }

    public async Task AddAsync(int userId, string badge) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            @"INSERT INTO user_badges (user_id, badge)
              VALUES (@UserId, @Badge::badge)
              ON CONFLICT (user_id, badge) DO NOTHING",
            new { UserId = userId, Badge = badge });
    }

    public async Task RemoveAsync(int userId, string badge) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            @"DELETE FROM user_badges
               WHERE user_id = @UserId
                 AND badge = @Badge::badge",
            new { UserId = userId, Badge = badge });
    }
}
