using Dapper;
using Npgsql;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Repositories;

[Collection("Postgres")]
public class DapperLocationRepositoryTests : IAsyncLifetime {
    private readonly PostgresFixture postgres;
    private NpgsqlDataSource dataSource = null!;

    public DapperLocationRepositoryTests(PostgresFixture postgres) {
        this.postgres = postgres;
    }

    public async Task InitializeAsync() {
        dataSource = new NpgsqlDataSourceBuilder(postgres.ConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("TRUNCATE TABLE locations RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() {
        dataSource?.Dispose();
        return Task.CompletedTask;
    }

    private DapperLocationRepository CreateNewRepository() => new(dataSource);

    [Fact]
    public async Task CreateAsyncReturnsNewIdGreaterThanZero() {
        var dapperLocationRepository = CreateNewRepository();
        var newId = await dapperLocationRepository.CreateAsync(NewLocation("Pallet Town"));
        Assert.True(newId > 0);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsInsertedRowAfterCreate() {
        var dapperLocationRepository = CreateNewRepository();
        var input = NewLocation("Viridian City", "First city", "https://example.com/v.mp4");
        var newId = await dapperLocationRepository.CreateAsync(input);

        var loaded = await dapperLocationRepository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Equal(newId, loaded!.LocationId);
        Assert.Equal("Viridian City", loaded.Name);
        Assert.Equal("First city", loaded.Description);
        Assert.Equal("https://example.com/v.mp4", loaded.VideoUrl);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullForMissingId() {
        var dapperLocationRepository = CreateNewRepository();
        var loaded = await dapperLocationRepository.GetByIdAsync(999_999);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetAllAsyncReturnsAllRowsOrderedByLocationId() {
        var dapperLocationRepository = CreateNewRepository();
        await dapperLocationRepository.CreateAsync(NewLocation("A"));
        await dapperLocationRepository.CreateAsync(NewLocation("B"));
        await dapperLocationRepository.CreateAsync(NewLocation("C"));

        var all = (await dapperLocationRepository.GetAllAsync()).ToList();

        Assert.Equal(3, all.Count);
        Assert.Equal(new[] { "A", "B", "C" }, all.Select(l => l.Name));
        Assert.True(all.SequenceEqual(all.OrderBy(l => l.LocationId)));
    }

    [Fact]
    public async Task UpdateAsyncMutatesExistingRowAndReturnsTrue() {
        var dapperLocationRepository = CreateNewRepository();
        var newId = await dapperLocationRepository.CreateAsync(NewLocation("Old name"));

        var updated = new Location {
            LocationId = newId,
            Name = "New name",
            Description = "Updated desc",
            VideoUrl = "https://example.com/new.mp4"
        };
        var ok = await dapperLocationRepository.UpdateAsync(updated);

        Assert.True(ok);
        var loaded = await dapperLocationRepository.GetByIdAsync(newId);
        Assert.Equal("New name", loaded!.Name);
        Assert.Equal("Updated desc", loaded.Description);
        Assert.Equal("https://example.com/new.mp4", loaded.VideoUrl);
    }

    [Fact]
    public async Task UpdateAsyncReturnsFalseWhenRowMissing() {
        var dapperLocationRepository = CreateNewRepository();
        var ok = await dapperLocationRepository.UpdateAsync(new Location {
            LocationId = 999_999,
            Name = "Ghost"
        });
        Assert.False(ok);
    }

    [Fact]
    public async Task DeleteAsyncRemovesExistingRowAndReturnsTrue() {
        var dapperLocationRepository = CreateNewRepository();
        var newId = await dapperLocationRepository.CreateAsync(NewLocation("Doomed"));

        var ok = await dapperLocationRepository.DeleteAsync(newId);

        Assert.True(ok);
        Assert.Null(await dapperLocationRepository.GetByIdAsync(newId));
    }

    [Fact]
    public async Task DeleteAsyncReturnsFalseWhenRowMissing() {
        var dapperLocationRepository = CreateNewRepository();
        var ok = await dapperLocationRepository.DeleteAsync(999_999);
        Assert.False(ok);
    }

    [Fact]
    public async Task NullDescriptionAndVideoUrlRoundTrip() {
        var dapperLocationRepository = CreateNewRepository();
        var newId = await dapperLocationRepository.CreateAsync(new Location { Name = "NullsOnly" });
        var loaded = await dapperLocationRepository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Null(loaded!.Description);
        Assert.Null(loaded.VideoUrl);
    }

    private static Location NewLocation(string name, string? description = null, string? videoUrl = null) =>
        new() { Name = name, Description = description, VideoUrl = videoUrl };
}
