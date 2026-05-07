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

    private async Task SeedImageAsync(int locationId, string imageUrl, int displayOrder, string? caption = null) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            @"INSERT INTO location_images (location_id, image_url, display_order, caption)
              VALUES (@LocationId, @ImageUrl, @DisplayOrder, @Caption)",
            new { LocationId = locationId, ImageUrl = imageUrl, DisplayOrder = displayOrder, Caption = caption });
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

    [Fact]
    public async Task GetByIdAsyncReturnsEmptyImagesWhenNoneExist() {
        var newId = await SeedLocationAsync("Pallet Town");
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Empty(loaded!.Images);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsImagesOrderedByDisplayOrder() {
        var newId = await SeedLocationAsync("Pewter City");
        await SeedImageAsync(newId, "/images/c.png", 3, "C");
        await SeedImageAsync(newId, "/images/a.png", 1, "A");
        await SeedImageAsync(newId, "/images/b.png", 2, "B");
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Equal(new[] { 1, 2, 3 }, loaded!.Images.Select(i => i.DisplayOrder));
        Assert.Equal(
            new[] { "/images/a.png", "/images/b.png", "/images/c.png" },
            loaded.Images.Select(i => i.ImageUrl));
        Assert.All(loaded.Images, i => Assert.Equal(newId, i.LocationId));
    }

    [Fact]
    public async Task GetByIdAsyncDoesNotReturnImagesFromOtherLocations() {
        var palletId = await SeedLocationAsync("Pallet Town");
        var pewterId = await SeedLocationAsync("Pewter City");
        await SeedImageAsync(palletId, "/images/pallet-1.png", 1);
        await SeedImageAsync(pewterId, "/images/pewter-1.png", 1);
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(palletId);

        Assert.NotNull(loaded);
        Assert.Single(loaded!.Images);
        Assert.Equal("/images/pallet-1.png", loaded.Images[0].ImageUrl);
    }

    [Fact]
    public async Task GetAllAsyncDoesNotPopulateImages() {
        var palletId = await SeedLocationAsync("Pallet Town");
        await SeedImageAsync(palletId, "/images/pallet-1.png", 1);
        var repository = CreateNewRepository();

        var all = (await repository.GetAllAsync()).ToList();

        Assert.Single(all);
        Assert.Empty(all[0].Images);
    }
}
