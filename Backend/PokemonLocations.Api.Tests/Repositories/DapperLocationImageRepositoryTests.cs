using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PokemonLocations.Api.Repositories;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Repositories;

[Collection("Postgres")]
public class DapperLocationImageRepositoryTests : IAsyncLifetime {
    private readonly PostgresFixture postgres;
    private NpgsqlDataSource dataSource = null!;

    public DapperLocationImageRepositoryTests(PostgresFixture postgres) {
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

    private DapperLocationImageRepository CreateNewRepository() {
        return new DapperLocationImageRepository(
            dataSource,
            NullLogger<DapperLocationImageRepository>.Instance);
    }

    private async Task<int> SeedLocationAsync(string name = "Test Town") {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            "INSERT INTO locations (name) VALUES (@Name) RETURNING location_id",
            new { Name = name });
    }

    private async Task<int> SeedImageAsync(
        int locationId,
        string imageUrl = "/images/test.png",
        int displayOrder = 0,
        string? caption = "A caption") {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO location_images (location_id, image_url, display_order, caption)
              VALUES (@LocationId, @ImageUrl, @DisplayOrder, @Caption)
              RETURNING image_id",
            new { LocationId = locationId, ImageUrl = imageUrl, DisplayOrder = displayOrder, Caption = caption });
    }

    [Fact]
    public async Task GetAllByLocationAsyncReturnsEmptyWhenNoImagesExist() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var images = await repository.GetAllByLocationAsync(locationId);

        Assert.Empty(images);
    }

    [Fact]
    public async Task GetAllByLocationAsyncReturnsOnlyImagesForGivenLocation() {
        var palletId = await SeedLocationAsync("Pallet Town");
        var pewterId = await SeedLocationAsync("Pewter City");
        await SeedImageAsync(palletId, "/images/pallet-1.png", 1);
        await SeedImageAsync(palletId, "/images/pallet-2.png", 2);
        await SeedImageAsync(pewterId, "/images/pewter-1.png", 1);
        var repository = CreateNewRepository();

        var palletImages = (await repository.GetAllByLocationAsync(palletId)).ToList();

        Assert.Equal(2, palletImages.Count);
        Assert.All(palletImages, i => Assert.Equal(palletId, i.LocationId));
    }

    [Fact]
    public async Task GetAllByLocationAsyncOrdersImagesByDisplayOrder() {
        var locationId = await SeedLocationAsync();
        await SeedImageAsync(locationId, "/images/c.png", 3);
        await SeedImageAsync(locationId, "/images/a.png", 1);
        await SeedImageAsync(locationId, "/images/b.png", 2);
        var repository = CreateNewRepository();

        var images = (await repository.GetAllByLocationAsync(locationId)).ToList();

        Assert.Equal(new[] { 1, 2, 3 }, images.Select(i => i.DisplayOrder));
        Assert.Equal(
            new[] { "/images/a.png", "/images/b.png", "/images/c.png" },
            images.Select(i => i.ImageUrl));
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullWhenImageDoesNotExist() {
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(999_999);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsImageWhenItExists() {
        var locationId = await SeedLocationAsync();
        var newId = await SeedImageAsync(locationId, "/images/overview.png", 1, "Overview");
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Equal(newId, loaded!.ImageId);
        Assert.Equal(locationId, loaded.LocationId);
        Assert.Equal("/images/overview.png", loaded.ImageUrl);
        Assert.Equal(1, loaded.DisplayOrder);
        Assert.Equal("Overview", loaded.Caption);
    }

    [Fact]
    public async Task NullCaptionRoundTrips() {
        var locationId = await SeedLocationAsync();
        var newId = await SeedImageAsync(locationId, caption: null);
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Null(loaded!.Caption);
    }
}
