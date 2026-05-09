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

    [Fact]
    public async Task GetForUserAndLocationAsyncReturnsImagesNewestFirst() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        var older = MakeImage(userId, 1) with { UploadedAt = DateTime.UtcNow.AddMinutes(-5) };
        var newer = MakeImage(userId, 1) with { UploadedAt = DateTime.UtcNow };
        await repository.AddAsync(older, 20);
        await repository.AddAsync(newer, 20);

        var images = await repository.GetForUserAndLocationAsync(userId, 1);

        Assert.Equal(2, images.Count);
        Assert.Equal(newer.ImageId, images[0].ImageId);
        Assert.Equal(older.ImageId, images[1].ImageId);
    }

    [Fact]
    public async Task GetForUserAndLocationAsyncIsScopedToLocation() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        await repository.AddAsync(MakeImage(userId, 1), 20);
        await repository.AddAsync(MakeImage(userId, 2), 20);

        var loc1 = await repository.GetForUserAndLocationAsync(userId, 1);

        Assert.Single(loc1);
        Assert.Equal(1, loc1[0].LocationId);
    }

    [Fact]
    public async Task GetForUserAndLocationAsyncDoesNotReturnAnotherUsersImages() {
        await ResetAsync();
        var redId = await SeedUserAsync("red@example.com");
        var blueId = await SeedUserAsync("blue@example.com");
        var repository = CreateRepository();
        await repository.AddAsync(MakeImage(redId, 1), 20);
        await repository.AddAsync(MakeImage(blueId, 1), 20);

        var redImages = await repository.GetForUserAndLocationAsync(redId, 1);

        Assert.Single(redImages);
        Assert.Equal(redId, redImages[0].UserId);
    }

    [Fact]
    public async Task RemoveAsyncDeletesOwnedImage() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        var image = MakeImage(userId, 1);
        await repository.AddAsync(image, 20);

        await repository.RemoveAsync(userId, image.ImageId);

        Assert.Null(await repository.GetByIdForUserAsync(userId, image.ImageId));
    }

    [Fact]
    public async Task RemoveAsyncIsIdempotentWhenRowMissing() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();

        await repository.RemoveAsync(userId, Guid.NewGuid()); // should not throw
    }

    [Fact]
    public async Task RemoveAsyncDoesNotDeleteAnotherUsersImage() {
        await ResetAsync();
        var redId = await SeedUserAsync("red@example.com");
        var blueId = await SeedUserAsync("blue@example.com");
        var repository = CreateRepository();
        var blueImage = MakeImage(blueId, 1);
        await repository.AddAsync(blueImage, 20);

        await repository.RemoveAsync(redId, blueImage.ImageId);

        var stillThere = await repository.GetByIdForUserAsync(blueId, blueImage.ImageId);
        Assert.NotNull(stillThere);
    }

    [Fact]
    public async Task CountForUserAndLocationAsyncReturnsScopedCount() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        await repository.AddAsync(MakeImage(userId, 1), 20);
        await repository.AddAsync(MakeImage(userId, 1), 20);
        await repository.AddAsync(MakeImage(userId, 2), 20);

        Assert.Equal(2, await repository.CountForUserAndLocationAsync(userId, 1));
        Assert.Equal(1, await repository.CountForUserAndLocationAsync(userId, 2));
        Assert.Equal(0, await repository.CountForUserAndLocationAsync(userId, 99));
    }

    [Fact]
    public async Task AddAsyncReturnsAtCapWhenLimitReached() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        for (int i = 0; i < 3; i++) {
            await repository.AddAsync(MakeImage(userId, 1), locationCap: 3);
        }

        var result = await repository.AddAsync(MakeImage(userId, 1), locationCap: 3);

        Assert.Equal(AddResult.AtCap, result);
        Assert.Equal(3, await repository.CountForUserAndLocationAsync(userId, 1));
    }

    [Fact]
    public async Task DeletingUserCascadesUserImages() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        await repository.AddAsync(MakeImage(userId, 1), 20);
        await repository.AddAsync(MakeImage(userId, 2), 20);

        await CreateUserRepository().DeleteAsync(userId);

        await using var connection = new NpgsqlConnection(postgresFixture.ConnectionString);
        await connection.OpenAsync();
        var orphans = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM user_images WHERE user_id = @UserId",
            new { UserId = userId });
        Assert.Equal(0, orphans);
    }
}
