using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests.Database;

[Collection("Postgres")]
public class VisitedBuildingRepositoryTests {
    private readonly PostgresFixture postgresFixture;

    public VisitedBuildingRepositoryTests(PostgresFixture postgresFixture) {
        this.postgresFixture = postgresFixture;
    }

    private VisitedBuildingRepository CreateRepository() {
        var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        return new VisitedBuildingRepository(dataSource);
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
        var repository = CreateRepository();

        var visited = await repository.GetForUserAsync(userId);

        Assert.Empty(visited);
    }

    [Fact]
    public async Task AddAsyncPersistsBuilding() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();

        await repository.AddAsync(userId, 7);
        var visited = await repository.GetForUserAsync(userId);

        Assert.Contains(7, visited);
    }

    [Fact]
    public async Task AddAsyncIsIdempotent() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();

        await repository.AddAsync(userId, 7);
        await repository.AddAsync(userId, 7);
        var visited = await repository.GetForUserAsync(userId);

        Assert.Single(visited);
    }

    [Fact]
    public async Task RemoveAsyncRemovesBuilding() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        await repository.AddAsync(userId, 7);

        await repository.RemoveAsync(userId, 7);

        Assert.Empty(await repository.GetForUserAsync(userId));
    }

    [Fact]
    public async Task RemoveAsyncIsNoOpForUnvisited() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();

        await repository.RemoveAsync(userId, 7);

        Assert.Empty(await repository.GetForUserAsync(userId));
    }

    [Fact]
    public async Task VisitsAreScopedPerUser() {
        await ResetAsync();
        var redId = await SeedUserAsync("red@example.com");
        var blueId = await SeedUserAsync("blue@example.com");
        var repository = CreateRepository();

        await repository.AddAsync(redId, 1);
        await repository.AddAsync(blueId, 2);

        Assert.Equal([1], await repository.GetForUserAsync(redId));
        Assert.Equal([2], await repository.GetForUserAsync(blueId));
    }

    [Fact]
    public async Task DeletingUserCascadesVisitedBuildings() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        await repository.AddAsync(userId, 1);
        await repository.AddAsync(userId, 2);

        await CreateUserRepository().DeleteAsync(userId);

        await using var connection = new NpgsqlConnection(postgresFixture.ConnectionString);
        await connection.OpenAsync();
        var orphanCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM user_visited_buildings WHERE user_id = @UserId",
            new { UserId = userId });
        Assert.Equal(0, orphanCount);
    }
}
