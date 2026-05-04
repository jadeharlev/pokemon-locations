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
}
