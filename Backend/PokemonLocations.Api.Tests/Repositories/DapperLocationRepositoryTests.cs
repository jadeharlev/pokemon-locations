using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
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

    private DapperLocationRepository CreateNewRepository() {
        return new DapperLocationRepository(
            dataSource,
            NullLogger<DapperLocationRepository>.Instance);
    }

    private async Task<int> SeedLocationAsync(string name, string? description = null, string? videoUrl = null) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO locations (name, description, video_url)
              VALUES (@Name, @Description, @VideoUrl)
              RETURNING location_id",
            new { Name = name, Description = description, VideoUrl = videoUrl });
    }

    [Fact]
    public async Task GetByIdAsyncReturnsSeededRow() {
        var newId = await SeedLocationAsync("Viridian City", "First city", "https://example.com/v.mp4");
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Equal(newId, loaded!.LocationId);
        Assert.Equal("Viridian City", loaded.Name);
        Assert.Equal("First city", loaded.Description);
        Assert.Equal("https://example.com/v.mp4", loaded.VideoUrl);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullForMissingId() {
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(999_999);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetAllAsyncReturnsAllRowsOrderedByLocationId() {
        await SeedLocationAsync("A");
        await SeedLocationAsync("B");
        await SeedLocationAsync("C");
        var repository = CreateNewRepository();

        var all = (await repository.GetAllAsync()).ToList();

        Assert.Equal(3, all.Count);
        Assert.Equal(new[] { "A", "B", "C" }, all.Select(l => l.Name));
        Assert.True(all.SequenceEqual(all.OrderBy(l => l.LocationId)));
    }

    [Fact]
    public async Task NullDescriptionAndVideoUrlRoundTrip() {
        var newId = await SeedLocationAsync("NullsOnly");
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Null(loaded!.Description);
        Assert.Null(loaded.VideoUrl);
    }
}
