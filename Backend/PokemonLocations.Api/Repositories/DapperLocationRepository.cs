using Dapper;
using Npgsql;
using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class DapperLocationRepository : ILocationRepository {
    private readonly NpgsqlDataSource dataSource;

    public DapperLocationRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<IEnumerable<Location>> GetAllAsync() {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QueryAsync<Location>(
            "SELECT location_id, name, description, video_url FROM locations ORDER BY location_id");
    }

    public async Task<Location?> GetByIdAsync(int locationId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Location>(
            "SELECT location_id, name, description, video_url FROM locations WHERE location_id = @LocationId",
            new { LocationId = locationId });
    }

    public async Task<int> CreateAsync(Location location) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO locations (name, description, video_url)
              VALUES (@Name, @Description, @VideoUrl)
              RETURNING location_id",
            location);
    }

    public async Task<bool> UpdateAsync(Location location) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.ExecuteAsync(
            @"UPDATE locations
                 SET name = @Name,
                     description = @Description,
                     video_url = @VideoUrl
               WHERE location_id = @LocationId",
            location);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int locationId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.ExecuteAsync(
            "DELETE FROM locations WHERE location_id = @LocationId",
            new { LocationId = locationId });
        return rows > 0;
    }
}
