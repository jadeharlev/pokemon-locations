using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests.Database;

[Collection("Postgres")]
public class UserRepositoryTests {
    private readonly PostgresFixture postgresFixture;

    public UserRepositoryTests(PostgresFixture postgresFixture) {
        this.postgresFixture = postgresFixture;
    }

    private UserRepository CreateRepository() {
        var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        return new UserRepository(dataSource);
    }

    private async Task ResetUsersAsync() {
        await using var connection = new NpgsqlConnection(postgresFixture.ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM users");
    }

    [Fact]
    public async Task CreateAsyncInsertsUserAndReturnsAssignedId() {
        await ResetUsersAsync();
        var repository = CreateRepository();

        var created = await repository.CreateAsync(
            email: "red@example.com",
            passwordHash: "hashed-pw",
            displayName: "Red");

        Assert.True(created.UserId > 0);
        Assert.Equal("red@example.com", created.Email);
        Assert.Equal("Red", created.DisplayName);
        Assert.Equal("bulbasaur", created.Theme);
    }

    [Fact]
    public async Task GetByEmailAsyncReturnsInsertedUser() {
        await ResetUsersAsync();
        var repository = CreateRepository();
        await repository.CreateAsync("red@example.com", "hashed-pw", "Red");

        var user = await repository.GetByEmailAsync("red@example.com");

        Assert.NotNull(user);
        Assert.Equal("Red", user!.DisplayName);
    }

    [Fact]
    public async Task GetByEmailAsyncReturnsNullForUnknownEmail() {
        await ResetUsersAsync();
        var repository = CreateRepository();

        var user = await repository.GetByEmailAsync("nobody@example.com");

        Assert.Null(user);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsInsertedUser() {
        await ResetUsersAsync();
        var repository = CreateRepository();
        var created = await repository.CreateAsync("red@example.com", "hashed-pw", "Red");

        var user = await repository.GetByIdAsync(created.UserId);

        Assert.NotNull(user);
        Assert.Equal("red@example.com", user!.Email);
    }

    [Fact]
    public async Task CreateAsyncRejectsDuplicateEmail() {
        await ResetUsersAsync();
        var repository = CreateRepository();
        await repository.CreateAsync("red@example.com", "hashed-pw", "Red");

        await Assert.ThrowsAsync<PostgresException>(() =>
            repository.CreateAsync("red@example.com", "other-hash", "Red Again"));
    }

    [Theory]
    [InlineData("bulbasaur")]
    [InlineData("charmander")]
    [InlineData("squirtle")]
    [InlineData("pikachu")]
    public async Task UpdateThemeAsyncPersistsTheme(string theme) {
        await ResetUsersAsync();
        var repository = CreateRepository();
        var created = await repository.CreateAsync("red@example.com", "hashed-pw", "Red");

        await repository.UpdateThemeAsync(created.UserId, theme);

        var updated = await repository.GetByIdAsync(created.UserId);
        Assert.NotNull(updated);
        Assert.Equal(theme, updated!.Theme);
    }

    [Fact]
    public async Task UpdateThemeAsyncRejectsUnknownTheme() {
        await ResetUsersAsync();
        var repository = CreateRepository();
        var created = await repository.CreateAsync("red@example.com", "hashed-pw", "Red");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.UpdateThemeAsync(created.UserId, "missingno"));
    }

    [Fact]
    public async Task DeleteAsyncRemovesUser() {
        await ResetUsersAsync();
        var repository = CreateRepository();
        var created = await repository.CreateAsync("red@example.com", "hashed-pw", "Red");

        await repository.DeleteAsync(created.UserId);

        Assert.Null(await repository.GetByIdAsync(created.UserId));
    }
}
