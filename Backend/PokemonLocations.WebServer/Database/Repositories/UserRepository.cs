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
              RETURNING user_id               AS UserId,
                        email                 AS Email,
                        password_hash         AS PasswordHash,
                        display_name          AS DisplayName,
                        theme::text           AS Theme,
                        permanent_planet_name AS PermanentPlanetName,
                        created_at            AS CreatedAt",
            new { Email = email, PasswordHash = passwordHash, DisplayName = displayName });
    }

    public async Task<User?> GetByEmailAsync(string email) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<User>(
            @"SELECT user_id               AS UserId,
                     email                 AS Email,
                     password_hash         AS PasswordHash,
                     display_name          AS DisplayName,
                     theme::text           AS Theme,
                     permanent_planet_name AS PermanentPlanetName,
                     created_at            AS CreatedAt
                FROM users
               WHERE email = @Email",
            new { Email = email });
    }

    public async Task<User?> GetByIdAsync(int userId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<User>(
            @"SELECT user_id               AS UserId,
                     email                 AS Email,
                     password_hash         AS PasswordHash,
                     display_name          AS DisplayName,
                     theme::text           AS Theme,
                     permanent_planet_name AS PermanentPlanetName,
                     created_at            AS CreatedAt
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

    public async Task UpdateThemeAsync(int userId, string theme) {
        if (!Themes.IsValid(theme)) {
            throw new ArgumentException($"Unknown theme: {theme}", nameof(theme));
        }
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "UPDATE users SET theme = @Theme::user_theme WHERE user_id = @UserId",
            new { UserId = userId, Theme = theme });
    }

    public async Task UpdatePermanentPlanetAsync(int userId, string? planetName) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "UPDATE users SET permanent_planet_name = @PlanetName WHERE user_id = @UserId",
            new { UserId = userId, PlanetName = planetName });
    }

    public async Task<(int MaxGymsComplete, int MaxLocationsVisited, int MaxBuildingsVisited)>
        BumpAndGetMaxStatsAsync(int userId, int gymsComplete, int locationsVisited, int buildingsVisited) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<(int, int, int)>(
            @"UPDATE users
                 SET max_gyms_complete     = GREATEST(max_gyms_complete,     @GymsComplete),
                     max_locations_visited = GREATEST(max_locations_visited, @LocationsVisited),
                     max_buildings_visited = GREATEST(max_buildings_visited, @BuildingsVisited)
               WHERE user_id = @UserId
              RETURNING max_gyms_complete, max_locations_visited, max_buildings_visited",
            new {
                UserId = userId,
                GymsComplete = gymsComplete,
                LocationsVisited = locationsVisited,
                BuildingsVisited = buildingsVisited
            });
    }
}
