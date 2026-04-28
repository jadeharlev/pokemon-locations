using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class DapperLocationImageRepository : ILocationImageRepository {
    private readonly NpgsqlDataSource dataSource;
    private readonly ILogger<DapperLocationImageRepository> logger;

    public DapperLocationImageRepository(
        NpgsqlDataSource dataSource,
        ILogger<DapperLocationImageRepository> logger) {
        this.dataSource = dataSource;
        this.logger = logger;
    }

    public async Task<IEnumerable<LocationImage>> GetAllByLocationAsync(int locationId) {
        logger.LogInformation("Getting all images for location {LocationId} from the database.", locationId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var images = await connection.QueryAsync<LocationImage>(
            @"SELECT image_id, location_id, image_url, display_order, caption
                FROM location_images
               WHERE location_id = @LocationId
               ORDER BY display_order",
            new { LocationId = locationId });

        logger.LogInformation("Successfully retrieved images for location {LocationId} from the database.", locationId);

        return images;
    }

    public async Task<LocationImage?> GetByIdAsync(int imageId) {
        logger.LogInformation("Getting image with ID {ImageId} from the database.", imageId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var image = await connection.QuerySingleOrDefaultAsync<LocationImage>(
            @"SELECT image_id, location_id, image_url, display_order, caption
                FROM location_images
               WHERE image_id = @ImageId",
            new { ImageId = imageId });

        if (image == null) {
            logger.LogWarning("Image with ID {ImageId} was not found in the database.", imageId);
            return null;
        }

        logger.LogInformation("Successfully retrieved image with ID {ImageId} from the database.", imageId);

        return image;
    }

    public async Task<int> CreateAsync(LocationImage image) {
        logger.LogInformation("Creating image for location {LocationId} in the database.", image.LocationId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var newImageId = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO location_images (location_id, image_url, display_order, caption)
              VALUES (@LocationId, @ImageUrl, @DisplayOrder, @Caption)
              RETURNING image_id",
            image);

        logger.LogInformation("Successfully created image with ID {ImageId} for location {LocationId}.", newImageId, image.LocationId);

        return newImageId;
    }

    public async Task<bool> UpdateAsync(LocationImage image) {
        logger.LogInformation("Updating image with ID {ImageId} in the database.", image.ImageId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var rows = await connection.ExecuteAsync(
            @"UPDATE location_images
                 SET location_id = @LocationId,
                     image_url = @ImageUrl,
                     display_order = @DisplayOrder,
                     caption = @Caption
               WHERE image_id = @ImageId",
            image);

        if (rows == 0) {
            logger.LogWarning("Cannot update image because image with ID {ImageId} was not found.", image.ImageId);
            return false;
        }

        logger.LogInformation("Successfully updated image with ID {ImageId}.", image.ImageId);

        return true;
    }

    public async Task<bool> DeleteAsync(int imageId) {
        logger.LogInformation("Deleting image with ID {ImageId} from the database.", imageId);

        await using var connection = await dataSource.OpenConnectionAsync();

        var rows = await connection.ExecuteAsync(
            "DELETE FROM location_images WHERE image_id = @ImageId",
            new { ImageId = imageId });

        if (rows == 0) {
            logger.LogWarning("Cannot delete image because image with ID {ImageId} was not found.", imageId);
            return false;
        }

        logger.LogInformation("Successfully deleted image with ID {ImageId}.", imageId);

        return true;
    }
}
