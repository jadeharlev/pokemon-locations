using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests.Database;

[Collection("Postgres")]
public class BadgeRepositoryTests {
    private readonly PostgresFixture postgresFixture;

    public BadgeRepositoryTests(PostgresFixture postgresFixture) {
        this.postgresFixture = postgresFixture;
    }

    private BadgeRepository CreateBadgeRepository() {
        var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        return new BadgeRepository(dataSource);
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

    [Fact]
    public async Task GetForUserAsyncReturnsEmptyForNewUser() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateBadgeRepository();

        var badges = await repository.GetForUserAsync(userId);

        Assert.Empty(badges);
    }

    [Fact]
    public async Task AddAsyncPersistsBadge() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateBadgeRepository();

        await repository.AddAsync(userId, "boulder");
        var badges = await repository.GetForUserAsync(userId);

        Assert.Contains("boulder", badges);
    }

    [Fact]
    public async Task AddAsyncIsIdempotent() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateBadgeRepository();

        await repository.AddAsync(userId, "boulder");
        await repository.AddAsync(userId, "boulder");
        var badges = await repository.GetForUserAsync(userId);

        Assert.Single(badges);
        Assert.Equal("boulder", badges[0]);
    }

    [Fact]
    public async Task RemoveAsyncRemovesBadge() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateBadgeRepository();
        await repository.AddAsync(userId, "boulder");

        await repository.RemoveAsync(userId, "boulder");
        var badges = await repository.GetForUserAsync(userId);

        Assert.Empty(badges);
    }

    [Fact]
    public async Task RemoveAsyncIsNoOpForUnearnedBadge() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateBadgeRepository();

        await repository.RemoveAsync(userId, "thunder");

        var badges = await repository.GetForUserAsync(userId);
        Assert.Empty(badges);
    }

    [Fact]
    public async Task BadgesAreScopedPerUser() {
        await ResetAsync();
        var redId = await SeedUserAsync("red@example.com");
        var blueId = await SeedUserAsync("blue@example.com");
        var repository = CreateBadgeRepository();

        await repository.AddAsync(redId, "boulder");
        await repository.AddAsync(blueId, "cascade");

        Assert.Equal(["boulder"], await repository.GetForUserAsync(redId));
        Assert.Equal(["cascade"], await repository.GetForUserAsync(blueId));
    }

    [Fact]
    public async Task DeletingUserCascadesBadges() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateBadgeRepository();
        await repository.AddAsync(userId, "boulder");
        await repository.AddAsync(userId, "cascade");

        var users = CreateUserRepository();
        await users.DeleteAsync(userId);

        await using var connection = new NpgsqlConnection(postgresFixture.ConnectionString);
        await connection.OpenAsync();
        var orphanCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM user_badges WHERE user_id = @UserId",
            new { UserId = userId });
        Assert.Equal(0, orphanCount);
    }
}
