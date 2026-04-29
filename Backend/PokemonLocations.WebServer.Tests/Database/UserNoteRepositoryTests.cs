using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests.Database;

[Collection("Postgres")]
public class UserNoteRepositoryTests {
    private readonly PostgresFixture postgresFixture;

    public UserNoteRepositoryTests(PostgresFixture postgresFixture) {
        this.postgresFixture = postgresFixture;
    }

    private UserNoteRepository CreateRepository() {
        var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        return new UserNoteRepository(dataSource);
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
    public async Task GetAsyncReturnsNullForNewUser() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();

        var note = await repository.GetAsync(userId, 1);

        Assert.Null(note);
    }

    [Fact]
    public async Task UpsertAsyncInsertsNote() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();

        await repository.UpsertAsync(userId, 42, "Love this town!");
        var note = await repository.GetAsync(userId, 42);

        Assert.Equal("Love this town!", note);
    }

    [Fact]
    public async Task UpsertAsyncUpdatesExistingNote() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();

        await repository.UpsertAsync(userId, 42, "First impression");
        await repository.UpsertAsync(userId, 42, "Updated thoughts");
        var note = await repository.GetAsync(userId, 42);

        Assert.Equal("Updated thoughts", note);
    }

    [Fact]
    public async Task DeleteAsyncRemovesNote() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        await repository.UpsertAsync(userId, 42, "Some note");

        await repository.DeleteAsync(userId, 42);
        var note = await repository.GetAsync(userId, 42);

        Assert.Null(note);
    }

    [Fact]
    public async Task DeleteAsyncIsNoOpForMissingNote() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();

        await repository.DeleteAsync(userId, 42);

        Assert.Null(await repository.GetAsync(userId, 42));
    }

    [Fact]
    public async Task NotesAreScopedPerUser() {
        await ResetAsync();
        var redId = await SeedUserAsync("red@example.com");
        var blueId = await SeedUserAsync("blue@example.com");
        var repository = CreateRepository();

        await repository.UpsertAsync(redId, 1, "Red's note");
        await repository.UpsertAsync(blueId, 1, "Blue's note");

        Assert.Equal("Red's note", await repository.GetAsync(redId, 1));
        Assert.Equal("Blue's note", await repository.GetAsync(blueId, 1));
    }

    [Fact]
    public async Task DeletingUserCascadesNotes() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        await repository.UpsertAsync(userId, 1, "Note 1");
        await repository.UpsertAsync(userId, 2, "Note 2");

        await CreateUserRepository().DeleteAsync(userId);

        await using var connection = new NpgsqlConnection(postgresFixture.ConnectionString);
        await connection.OpenAsync();
        var orphanCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM user_location_notes WHERE user_id = @UserId",
            new { UserId = userId });
        Assert.Equal(0, orphanCount);
    }
}
