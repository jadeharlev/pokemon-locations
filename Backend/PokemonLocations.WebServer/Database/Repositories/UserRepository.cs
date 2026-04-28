using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Database.Repositories;

public class UserRepository : IUserRepository {
    private readonly NpgsqlDataSource dataSource;

    public UserRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<User> CreateAsync(string email, string passwordHash, string displayName) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<User>(
            @"INSERT INTO users (email, password_hash, display_name)
              VALUES (@Email, @PasswordHash, @DisplayName)
              RETURNING user_id        AS UserId,
                        email          AS Email,
                        password_hash  AS PasswordHash,
                        display_name   AS DisplayName,
                        theme::text    AS Theme,
                        created_at     AS CreatedAt",
            new { Email = email, PasswordHash = passwordHash, DisplayName = displayName });
    }

    public async Task<User?> GetByEmailAsync(string email) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<User>(
            @"SELECT user_id        AS UserId,
                     email          AS Email,
                     password_hash  AS PasswordHash,
                     display_name   AS DisplayName,
                     theme::text    AS Theme,
                     created_at     AS CreatedAt
                FROM users
               WHERE email = @Email",
            new { Email = email });
    }

    public async Task<User?> GetByIdAsync(int userId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<User>(
            @"SELECT user_id        AS UserId,
                     email          AS Email,
                     password_hash  AS PasswordHash,
                     display_name   AS DisplayName,
                     theme::text    AS Theme,
                     created_at     AS CreatedAt
                FROM users
               WHERE user_id = @UserId",
            new { UserId = userId });
    }

    public async Task DeleteAsync(int userId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "DELETE FROM users WHERE user_id = @UserId",
            new { UserId = userId });
    }
}
