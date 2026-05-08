using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Models;
using System.Data;

namespace PokemonLocations.WebServer.Database.Repositories;

public class UserImageRepository : IUserImageRepository {
    private readonly NpgsqlDataSource dataSource;

    public UserImageRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<AddResult> AddAsync(UserImage image, int locationCap) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        var current = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM user_images
               WHERE user_id = @UserId AND location_id = @LocationId",
            new { image.UserId, image.LocationId },
            tx);

        if (current >= locationCap) {
            await tx.RollbackAsync();
            return AddResult.AtCap;
        }

        await connection.ExecuteAsync(
            @"INSERT INTO user_images (
                  image_id, user_id, location_id, file_path,
                  original_filename, content_type, byte_size, uploaded_at)
              VALUES (
                  @ImageId, @UserId, @LocationId, @FilePath,
                  @OriginalFilename, @ContentType, @ByteSize, @UploadedAt)",
            image,
            tx);

        await tx.CommitAsync();
        return AddResult.Success;
    }

    public async Task<UserImage?> GetByIdForUserAsync(int userId, Guid imageId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<UserImage>(
            @"SELECT image_id          AS ""ImageId"",
                     user_id           AS ""UserId"",
                     location_id       AS ""LocationId"",
                     file_path         AS ""FilePath"",
                     original_filename AS ""OriginalFilename"",
                     content_type      AS ""ContentType"",
                     byte_size         AS ""ByteSize"",
                     uploaded_at       AS ""UploadedAt""
                FROM user_images
               WHERE user_id = @UserId AND image_id = @ImageId",
            new { UserId = userId, ImageId = imageId });
    }

    public async Task<IReadOnlyList<UserImage>> GetForUserAndLocationAsync(int userId, int locationId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<UserImage>(
            @"SELECT image_id          AS ""ImageId"",
                     user_id           AS ""UserId"",
                     location_id       AS ""LocationId"",
                     file_path         AS ""FilePath"",
                     original_filename AS ""OriginalFilename"",
                     content_type      AS ""ContentType"",
                     byte_size         AS ""ByteSize"",
                     uploaded_at       AS ""UploadedAt""
                FROM user_images
               WHERE user_id = @UserId AND location_id = @LocationId
               ORDER BY uploaded_at DESC",
            new { UserId = userId, LocationId = locationId });
        return rows.ToList();
    }
    public Task RemoveAsync(int userId, Guid imageId) =>
        throw new NotImplementedException();
    public Task<int> CountForUserAndLocationAsync(int userId, int locationId) =>
        throw new NotImplementedException();
}
