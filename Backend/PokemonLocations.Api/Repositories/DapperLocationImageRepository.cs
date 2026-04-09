using Dapper;
using Npgsql;
using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class DapperLocationImageRepository : ILocationImageRepository {
    private readonly NpgsqlDataSource dataSource;

    public DapperLocationImageRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<IEnumerable<LocationImage>> GetAllByLocationAsync(int locationId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QueryAsync<LocationImage>(
            @"SELECT image_id, location_id, image_url, display_order, caption
                FROM location_images
               WHERE location_id = @LocationId
               ORDER BY display_order",
            new { LocationId = locationId });
    }

    public async Task<LocationImage?> GetByIdAsync(int imageId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<LocationImage>(
            @"SELECT image_id, location_id, image_url, display_order, caption
                FROM location_images
               WHERE image_id = @ImageId",
            new { ImageId = imageId });
    }

    public async Task<int> CreateAsync(LocationImage image) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO location_images (location_id, image_url, display_order, caption)
              VALUES (@LocationId, @ImageUrl, @DisplayOrder, @Caption)
              RETURNING image_id",
            image);
    }

    public async Task<bool> UpdateAsync(LocationImage image) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.ExecuteAsync(
            @"UPDATE location_images
                 SET location_id = @LocationId,
                     image_url = @ImageUrl,
                     display_order = @DisplayOrder,
                     caption = @Caption
               WHERE image_id = @ImageId",
            image);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int imageId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.ExecuteAsync(
            "DELETE FROM location_images WHERE image_id = @ImageId",
            new { ImageId = imageId });
        return rows > 0;
    }
}
