using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests.Database;

[Collection("Postgres")]
public class UserImageRepositoryTests {
    private readonly PostgresFixture postgresFixture;

    public UserImageRepositoryTests(PostgresFixture postgresFixture) {
        this.postgresFixture = postgresFixture;
    }

    private UserImageRepository CreateRepository() {
        var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        return new UserImageRepository(dataSource);
    }

    private UserRepository CreateUserRepository() {
        var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        return new UserRepository(dataSource);
    }

    private async Task ResetAsync() {
        await using var connection = new NpgsqlConnection(postgresFixture.ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM users");
    }

    private async Task<int> SeedUserAsync(string email = "red@example.com") {
        var users = CreateUserRepository();
        var user = await users.CreateAsync(email, "hashed-pw", "Red");
        return user.UserId;
    }

    private static UserImage MakeImage(int userId, int locationId, Guid? imageId = null) =>
        new(
            ImageId: imageId ?? Guid.NewGuid(),
            UserId: userId,
            LocationId: locationId,
            FilePath: $"/app/uploads/{userId}/{Guid.NewGuid()}.png",
            OriginalFilename: "screenshot.png",
            ContentType: "image/png",
            ByteSize: 12345,
            UploadedAt: DateTime.UtcNow);

    [Fact]
    public async Task AddAsyncReturnsSuccessAndPersistsRow() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        var image = MakeImage(userId, locationId: 1);

        var result = await repository.AddAsync(image, locationCap: 20);

        Assert.Equal(AddResult.Success, result);
        var loaded = await repository.GetByIdForUserAsync(userId, image.ImageId);
        Assert.NotNull(loaded);
        Assert.Equal(image.OriginalFilename, loaded!.OriginalFilename);
        Assert.Equal(image.ContentType, loaded.ContentType);
        Assert.Equal(image.ByteSize, loaded.ByteSize);
        Assert.Equal(image.FilePath, loaded.FilePath);
    }
}
